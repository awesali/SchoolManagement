// Backend section: database queries and persistence.
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    // Reads and updates class data.
    public class ClassRepository : IClassRepository
    {
        private static string ClassNameKey(string name)
        {
            var key = System.Text.RegularExpressions.Regex.Replace(
                (name ?? "").Trim().ToLowerInvariant(),
                @"^(class|grade|std\.?|standard)\s*[-:]?\s*",
                ""
            );
            key = System.Text.RegularExpressions.Regex.Replace(key, @"\s+", "");
            return System.Text.RegularExpressions.Regex.Replace(key, @"^(\d+)(st|nd|rd|th)$", "$1");
        }

        // Dependencies and state used by this component.
        private readonly AppDbContext _context;

        // Creates the component with its required dependencies.
        public ClassRepository(AppDbContext context)
        {
            _context = context;
        }

        // Repository operations for querying and updating stored data.
        public async Task<ApiResponse<string>> CreateClassWithSectionsAsync(
            CreateClassWithSectionsDto dto
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );

            try
            {
                var names = await _context
                    .Classes.Where(c => c.SchoolId == dto.SchoolId)
                    .Select(c => c.ClassName)
                    .ToListAsync();
                if (names.Any(name => ClassNameKey(name) == ClassNameKey(dto.ClassName)))
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message =
                            "A class with this name already exists in this school. Please use a different name.",
                        Data = null,
                    };
                var newClass = new Classes
                {
                    ClassName = dto.ClassName.Trim(),
                    SchoolId = dto.SchoolId,
                    Created_Date = DateTime.UtcNow,
                    IsActive = true,
                };

                _context.Classes.Add(newClass);
                await _context.SaveChangesAsync();

                if (dto.Sections != null && dto.Sections.Any())
                {
                    var sections = dto
                        .Sections.Select(sec => new SectionDetails
                        {
                            SectionName = sec.SectionName,
                            ClassId = newClass.Id,
                            SchoolId = dto.SchoolId,
                            StaffId = sec.StaffId,
                            Created_Date = DateTime.UtcNow,
                            IsActive = true,
                        })
                        .ToList();

                    _context.SectionDetails.AddRange(sections);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Class with sections created successfully",
                    Data = null,
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to create class",
                    Data = null,
                };
            }
        }

        public Task<List<ClassDetailDto>> GetClassDetailsBySchoolIdAsync(int schoolId) =>
            LoadClassDetailsAsync(_context.Classes.Where(c => c.SchoolId == schoolId && c.IsActive)
                .OrderByDescending(c => c.Created_Date).ThenBy(c => c.Id));

        public async Task<(List<ClassDetailDto> Data, int TotalRecords)> GetClassDetailsPagedAsync(
            int schoolId, int page, int pageSize)
        {
            var query = _context.Classes.Where(c => c.SchoolId == schoolId && c.IsActive);
            var total = await query.CountAsync();
            var data = await LoadClassDetailsAsync(query.OrderByDescending(c => c.Created_Date)
                .ThenBy(c => c.Id).Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize));
            return (data, total);
        }

        public async Task<List<ClassDetailDto>> GetTeacherClassesAsync(int userId)
        {
            var staffId = await _context.Staff.Where(s => EF.Property<int?>(s, nameof(Staff.usersid)) == userId)
                .Select(s => (int?)s.Id).FirstOrDefaultAsync();
            if (!staffId.HasValue) return new();
            return await LoadClassDetailsAsync(_context.Classes.Where(c => c.IsActive &&
                _context.SectionDetails.Any(s => s.ClassId == c.Id && s.StaffId == staffId && s.IsActive))
                .OrderBy(c => c.ClassName).ThenBy(c => c.Id), staffId);
        }

        private async Task<List<ClassDetailDto>> LoadClassDetailsAsync(IQueryable<Classes> query, int? staffId = null)
        {
            // Three bounded queries avoid multiplying classes by every section and subject in SQL.
            var classes = await query.AsNoTracking().Select(c => new ClassDetailDto
            {
                Id = c.Id, ClassName = c.ClassName, SchoolId = c.SchoolId,
                CreatedDate = c.Created_Date, IsActive = c.IsActive, Sections = new(),
            }).ToListAsync();
            var classIds = classes.Select(c => c.Id).ToArray();
            if (classIds.Length == 0) return classes;
            var sections = await _context.SectionDetails.AsNoTracking()
                .Where(s => classIds.Contains(s.ClassId) && s.IsActive && (!staffId.HasValue || s.StaffId == staffId))
                .OrderBy(s => s.Id)
                .Select(s => new { s.ClassId, Section = new GetSectionDto
                {
                    Id = s.Id, SectionName = s.SectionName, StaffId = s.StaffId ?? 0,
                    MonitorStudentId = s.MonitorStudentId, Subjects = new(),
                } }).ToListAsync();
            var sectionIds = sections.Select(s => s.Section.Id).ToArray();
            var subjects = await (from link in _context.SectionSubjects.AsNoTracking()
                join subject in _context.Subjects on link.SubjectId equals subject.Id
                where sectionIds.Contains(link.SectionId) && link.IsActive
                select new { link.SectionId, Subject = new SectionSubjectDto
                {
                    SubjectId = subject.Id, SubjectName = subject.SubjectName,
                    TeacherId = staffId.HasValue ? null : _context.SectionSubjectTeachers
                        .Where(m => m.SectionId == link.SectionId && m.SubjectId == subject.Id && m.IsActive)
                        .Select(m => (int?)m.StaffId).FirstOrDefault(),
                } }).ToListAsync();
            var bySection = subjects.ToLookup(s => s.SectionId, s => s.Subject);
            foreach (var section in sections) section.Section.Subjects = bySection[section.Section.Id].ToList();
            var byClass = sections.ToLookup(s => s.ClassId, s => s.Section);
            foreach (var classroom in classes)
            {
                classroom.Sections = byClass[classroom.Id].ToList();
                classroom.SectionCount = classroom.Sections.Count;
            }
            return classes;
        }

        public async Task<ApiResponse<string>> UpdateClassWithSectionsAsync(
            UpdateClassWithSectionsDto dto
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );

            try
            {
                var existingClass = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.Id == dto.ClassId && c.IsActive
                );

                if (existingClass == null)
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Class not found",
                        Data = null,
                    };

                var otherNames = await _context
                    .Classes.Where(c => c.SchoolId == existingClass.SchoolId && c.Id != dto.ClassId)
                    .Select(c => c.ClassName)
                    .ToListAsync();
                var duplicateClass = otherNames.Any(name =>
                    ClassNameKey(name) == ClassNameKey(dto.ClassName)
                );

                if (duplicateClass)
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Class already exists",
                        Data = null,
                    };

                existingClass.ClassName = dto.ClassName.Trim();
                existingClass.Modified_Date = DateTime.UtcNow;

                var existingSections = await _context
                    .SectionDetails.Where(s => s.ClassId == dto.ClassId && s.IsActive)
                    .ToListAsync();

                foreach (var sec in dto.Sections)
                {
                    var isDuplicateSection = existingSections.Any(s =>
                        s.SectionName == sec.SectionName && (!sec.Id.HasValue || s.Id != sec.Id)
                    );

                    if (isDuplicateSection)
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = $"Section '{sec.SectionName}' already exists",
                            Data = null,
                        };

                    if (sec.Id.HasValue)
                    {
                        var existingSection = existingSections.FirstOrDefault(s =>
                            s.Id == sec.Id.Value
                        );
                        if (existingSection != null)
                        {
                            existingSection.SectionName = sec.SectionName;
                            existingSection.StaffId = sec.StaffId;
                            existingSection.Modified_Date = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        await _context.SectionDetails.AddAsync(
                            new SectionDetails
                            {
                                ClassId = dto.ClassId,
                                SchoolId = existingClass.SchoolId,
                                SectionName = sec.SectionName,
                                StaffId = sec.StaffId,
                                Created_Date = DateTime.UtcNow,
                                IsActive = true,
                            }
                        );
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Class and sections updated successfully",
                    Data = null,
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null,
                };
            }
        }

        public async Task<ApiResponse<List<SectionSubjectDto>>> GetSubjectsBySectionIdAsync(
            int sectionId
        )
        {
            var sectionExists = await _context.SectionDetails.AnyAsync(s =>
                s.Id == sectionId && s.IsActive
            );
            if (!sectionExists)
                return new ApiResponse<List<SectionSubjectDto>>
                {
                    Success = false,
                    Message = "Section not found",
                    Data = null,
                };

            var subjects = await _context
                .SectionSubjects.Where(ss => ss.SectionId == sectionId && ss.IsActive)
                .Join(
                    _context.Subjects,
                    ss => ss.SubjectId,
                    sub => sub.Id,
                    (ss, sub) =>
                        new SectionSubjectDto
                        {
                            TeacherId = _context
                                .SectionSubjectTeachers.Where(mapping =>
                                    mapping.SectionId == ss.SectionId
                                    && mapping.SubjectId == sub.Id
                                    && mapping.IsActive
                                )
                                .Select(mapping => (int?)mapping.StaffId)
                                .FirstOrDefault(),
                            SubjectId = sub.Id,
                            SubjectName = sub.SubjectName,
                        }
                )
                .ToListAsync();

            return new ApiResponse<List<SectionSubjectDto>>
            {
                Success = true,
                Message = "Subjects fetched successfully",
                Data = subjects,
            };
        }
    }
}
