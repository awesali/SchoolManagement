using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    public class ClassRepository : IClassRepository
    {
        private readonly AppDbContext _context;
        public ClassRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<string>> CreateClassWithSectionsAsync(CreateClassWithSectionsDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newClass = new Classes
                {
                    ClassName = dto.ClassName,
                    SchoolId = dto.SchoolId,
                    Created_Date = DateTime.UtcNow,
                    IsActive = true,
                };

                _context.Classes.Add(newClass);
                await _context.SaveChangesAsync();

                if (dto.Sections != null && dto.Sections.Any())
                {
                    var sections = dto.Sections.Select(sec => new SectionDetails
                    {
                        SectionName = sec.SectionName,
                        ClassId = newClass.Id,
                        SchoolId = dto.SchoolId,
                        StaffId = sec.StaffId,
                        Created_Date = DateTime.UtcNow,
                        IsActive = true
                    }).ToList();

                    _context.SectionDetails.AddRange(sections);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Class with sections created successfully", Data = null };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = "Failed to create class", Data = null };
            }
        }
        public async Task<List<ClassDetailDto>> GetClassDetailsBySchoolIdAsync(int schoolId)
        {
            return await _context.Classes
                .Where(c => c.SchoolId == schoolId && c.IsActive)
                .Select(c => new ClassDetailDto
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    SchoolId = c.SchoolId,
                    CreatedDate = c.Created_Date,
                    IsActive = c.IsActive,

                    SectionCount = _context.SectionDetails
                        .Count(s => s.ClassId == c.Id && s.IsActive),

                    // 🔥 Sections list
                    Sections = _context.SectionDetails
                        .Where(s => s.ClassId == c.Id && s.IsActive)
                        .Select(s => new GetSectionDto
                        {
                            Id = s.Id,
                            SectionName = s.SectionName,
                            StaffId = s.StaffId,
                            MonitorStudentId = s.MonitorStudentId,
                            Subjects = _context.SectionSubjects
                                .Where(ss => ss.SectionId == s.Id && ss.IsActive)
                                .Join(_context.Subjects, ss => ss.SubjectId, sub => sub.Id, (ss, sub) => new SectionSubjectDto
                                {
                                    SubjectId = sub.Id,
                                    SubjectName = sub.SubjectName
                                }).ToList()
                        }).ToList()
                })
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<(List<ClassDetailDto> Data, int TotalRecords)> GetClassDetailsPagedAsync(int schoolId, int page, int pageSize)
        {
            var query = _context.Classes
                .Where(c => c.SchoolId == schoolId && c.IsActive)
                .OrderByDescending(c => c.Created_Date);

            var total = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClassDetailDto
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    SchoolId = c.SchoolId,
                    CreatedDate = c.Created_Date,
                    IsActive = c.IsActive,
                    SectionCount = _context.SectionDetails.Count(s => s.ClassId == c.Id && s.IsActive),
                    Sections = _context.SectionDetails
                        .Where(s => s.ClassId == c.Id && s.IsActive)
                        .Select(s => new GetSectionDto
                        {
                            Id = s.Id,
                            SectionName = s.SectionName,
                            StaffId = s.StaffId,
                            MonitorStudentId = s.MonitorStudentId,
                            Subjects = _context.SectionSubjects
                                .Where(ss => ss.SectionId == s.Id && ss.IsActive)
                                .Join(_context.Subjects, ss => ss.SubjectId, sub => sub.Id, (ss, sub) => new SectionSubjectDto
                                {
                                    SubjectId = sub.Id,
                                    SubjectName = sub.SubjectName
                                }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return (data, total);
        }

        public async Task<ApiResponse<string>> UpdateClassWithSectionsAsync(UpdateClassWithSectionsDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == dto.ClassId && c.IsActive);

                if (existingClass == null)
                    return new ApiResponse<string> { Success = false, Message = "Class not found", Data = null };

                var duplicateClass = await _context.Classes.AnyAsync(c =>
                    c.ClassName == dto.ClassName &&
                    c.SchoolId == existingClass.SchoolId &&
                    c.Id != dto.ClassId &&
                    c.IsActive);

                if (duplicateClass)
                    return new ApiResponse<string> { Success = false, Message = "Class already exists", Data = null };

                existingClass.ClassName = dto.ClassName;
                existingClass.Modified_Date = DateTime.UtcNow;

                var existingSections = await _context.SectionDetails
                    .Where(s => s.ClassId == dto.ClassId && s.IsActive)
                    .ToListAsync();

                foreach (var sec in dto.Sections)
                {
                    var isDuplicateSection = existingSections.Any(s =>
                        s.SectionName == sec.SectionName &&
                        (!sec.Id.HasValue || s.Id != sec.Id));

                    if (isDuplicateSection)
                        return new ApiResponse<string> { Success = false, Message = $"Section '{sec.SectionName}' already exists", Data = null };

                    if (sec.Id.HasValue)
                    {
                        var existingSection = existingSections.FirstOrDefault(s => s.Id == sec.Id.Value);
                        if (existingSection != null)
                        {
                            existingSection.SectionName = sec.SectionName;
                            existingSection.StaffId = sec.StaffId;
                            existingSection.Modified_Date = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        await _context.SectionDetails.AddAsync(new SectionDetails
                        {
                            ClassId = dto.ClassId,
                            SectionName = sec.SectionName,
                            StaffId = sec.StaffId,
                            Created_Date = DateTime.UtcNow,
                            IsActive = true
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Class and sections updated successfully", Data = null };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = "Update failed", Data = null };
            }
        }

    }
}
