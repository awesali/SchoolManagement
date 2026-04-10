using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly AppDbContext _context;
        public SubjectRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<(List<SubjectDto> Data, int TotalRecords)> GetSubjectsBySchoolIdAsync(int schoolId, int page, int pageSize)
        {
            var query = from s in _context.Subjects
                        join st in _context.SubjectTeachers on s.Id equals st.SubjectId into stGroup
                        from st in stGroup.DefaultIfEmpty()
                        join staff in _context.Staff on st.StaffId equals staff.Id into staffGroup
                        from staff in staffGroup.DefaultIfEmpty()
                        where s.SchoolId == schoolId && s.IsActive
                        orderby s.Id descending
                        select new SubjectDto
                        {
                            Id = s.Id,
                            SubjectName = s.SubjectName,
                            SchoolId = s.SchoolId,
                            Created_Date = s.Created_Date,
                            Modified_Date = s.Modified_Date,
                            IsActive = s.IsActive,
                            TeacherId = staff != null ? staff.Id : (int?)null,
                            TeacherName = staff != null ? staff.Name : null
                        };

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, total);
        }
        public async Task<ApiResponse<SubjectDto>> AddSubjectAsync(AddSubjectDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var duplicate = await _context.Subjects
                    .AnyAsync(s => s.SubjectName == dto.SubjectName && s.SchoolId == dto.SchoolId && s.IsActive);

                if (duplicate)
                    return new ApiResponse<SubjectDto> { Success = false, Message = "Subject already exists", Data = null };

                var subject = new Subjects
                {
                    SubjectName = dto.SubjectName,
                    SchoolId = dto.SchoolId,
                    Created_Date = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                _context.SubjectTeachers.Add(new SubjectTeachers
                {
                    SubjectId = subject.Id,
                    StaffId = dto.StaffId,
                    SchoolId = dto.SchoolId,
                    Created_Date = DateTime.UtcNow,
                    IsActive = true
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var teacher = await _context.Staff.FirstOrDefaultAsync(s => s.Id == dto.StaffId);

                return new ApiResponse<SubjectDto>
                {
                    Success = true,
                    Message = "Subject added successfully",
                    Data = new SubjectDto
                    {
                        Id = subject.Id,
                        SubjectName = subject.SubjectName,
                        SchoolId = subject.SchoolId,
                        Created_Date = subject.Created_Date,
                        IsActive = subject.IsActive,
                        TeacherId = teacher?.Id,
                        TeacherName = teacher?.Name
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<SubjectDto> { Success = false, Message = ex.Message, Data = null };
            }
        }
        public async Task<ApiResponse<string>> UpdateSubjectAsync(UpdateSubjectDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == dto.Id && s.IsActive);
                if (subject == null)
                    return new ApiResponse<string> { Success = false, Message = "Subject not found", Data = null };

                var duplicate = await _context.Subjects
                    .AnyAsync(s => s.SubjectName == dto.SubjectName && s.SchoolId == subject.SchoolId && s.Id != dto.Id && s.IsActive);

                if (duplicate)
                    return new ApiResponse<string> { Success = false, Message = "Subject name already exists", Data = null };

                subject.SubjectName = dto.SubjectName;
                subject.Modified_Date = DateTime.UtcNow;

                var subjectTeacher = await _context.SubjectTeachers.FirstOrDefaultAsync(st => st.SubjectId == dto.Id && st.IsActive);
                if (subjectTeacher != null)
                {
                    subjectTeacher.StaffId = dto.StaffId;
                    subjectTeacher.Modified_Date = DateTime.UtcNow;
                }
                else
                {
                    _context.SubjectTeachers.Add(new SubjectTeachers
                    {
                        SubjectId = dto.Id,
                        StaffId = dto.StaffId,
                        SchoolId = subject.SchoolId,
                        Created_Date = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Subject updated successfully", Data = null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = ex.Message, Data = null };
            }
        }

        public async Task<ApiResponse<string>> AssignSubjectsToSectionAsync(AssignSubjectToSectionDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sectionExists = await _context.SectionDetails.AnyAsync(s => s.Id == dto.SectionId && s.IsActive);
                if (!sectionExists)
                    return new ApiResponse<string> { Success = false, Message = "Section not found", Data = null };

                var existing = await _context.SectionSubjects.Where(ss => ss.SectionId == dto.SectionId && ss.IsActive).ToListAsync();
                existing.ForEach(ss => { ss.IsActive = false; ss.Modified_Date = DateTime.UtcNow; });

                _context.SectionSubjects.AddRange(dto.SubjectIds.Distinct().Select(subjectId => new SectionSubjects
                {
                    SectionId = dto.SectionId,
                    SubjectId = subjectId,
                    SchoolId = dto.SchoolId,
                    Created_Date = DateTime.UtcNow,
                    IsActive = true
                }).ToList());

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Subjects assigned to section successfully", Data = null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = ex.Message, Data = null };
            }
        }

    }
}
