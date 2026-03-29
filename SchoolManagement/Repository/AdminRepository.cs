using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;

        public AdminRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Schools> CreateSchool(SchoolCreateDto dto, int userId)
        {
            var school = new Schools
            {
                SchoolName = dto.SchoolName,
                Address = dto.Address,
                Email = dto.Email,
                Phone = dto.Phone,
                SuperAdminId = userId,
                Created_By = userId,
                Created_Date = DateTime.Now,
                IsActive = true
            };

            _context.Schools.Add(school);

            await _context.SaveChangesAsync();

            return school;
        }
        public async Task<DashboardCardDto> GetDashboardData(int schoolId)
        {
            var today = DateTime.Today;

            // Total Teachers
            var totalTeachers = await _context.Staff
                .Where(x => x.SchoolId == schoolId && x.IsActive)
                .CountAsync();

            // Teachers Present Today
            var teachersPresent = await _context.StaffAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Present")
                .CountAsync();

            // Total Students
            var totalStudents = await _context.Students
                .Where(x => x.SchoolId == schoolId && x.IsActive)
                .CountAsync();

            // Students Present
            var studentsPresent = await _context.StudentAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Present")
                .CountAsync();

            // Employees On Leave
            var employeesOnLeave = await _context.StaffAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Leave")
                .CountAsync();

            return new DashboardCardDto
            {
                TeachersPresentToday = $"{teachersPresent}/{totalTeachers}",
                StudentsPresentToday = $"{studentsPresent}/{totalStudents}",
                TotalEmployees = totalTeachers,
                EmployeesOnLeave = employeesOnLeave
            };
        }
        public async Task<List<Schools>> GetSchoolsBySuperAdminIdAsync(int superAdminId)
        {
            return await _context.Schools
                .Where(s => s.SuperAdminId == superAdminId && s.IsActive)
                .ToListAsync();
        }
        public async Task<List<StaffListDto>> GetStaffFullAsync(int schoolId)
        {
            return await (from s in _context.Staff
                          join r in _context.Roles on s.RoleId equals r.Id
                          join sc in _context.Schools on s.SchoolId equals sc.Id
                          where s.SchoolId == schoolId && s.IsActive
                          select new StaffListDto
                          {
                              Id = s.Id,
                              Name = s.Name,
                              Email = s.Email,
                              Phone = s.Phone,
                              DOB = s.DOB,
                              DOJ = s.DOJ,

                              RoleId = r.Id,
                              RoleName = r.RoleName,

                              SchoolName = sc.SchoolName
                          }).ToListAsync();
        }

        public async Task<Staff> AddStaffAsync(AddStaffDto dto)
        {
            var staff = new Staff
            {
                Name = dto.Name,
                DOB = dto.DOB,
                DOJ = dto.DOJ,
                RoleId = dto.RoleId,
                SchoolId = dto.SchoolId,
                Email = dto.Email,
                Phone = dto.Phone,
                Adress = dto.Address,
                IsActive = true,
                Created_Date = DateTime.UtcNow
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return staff;
        }
        public async Task<List<RoleDto>> GetRolesBySchoolIdAsync()
        {
            return await _context.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                })
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<List<StudentDto>> GetStudentsBySchoolIdAsync(int schoolId)
        {
            var students = await (
                from s in _context.Students

                join se in _context.StudentEnrollment
                    on s.Id equals se.StudentId into seGroup
                from se in seGroup.DefaultIfEmpty()

                join c in _context.Classes
                    on se.ClassId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()

                join sd in _context.SectionDetails
                    on se.SectionId equals sd.Id into sdGroup
                from sd in sdGroup.DefaultIfEmpty()

                join ac in _context.AcademicSessions
                on s.SchoolId equals ac.SchoolId into acGroup
                from ac in acGroup.DefaultIfEmpty()
                where s.SchoolId == schoolId

                select new StudentDto
                {
                    Id = s.Id,
                    StudentName = s.StudentName,
                    DOB = s.DOB,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    ParentId = s.ParentId,
                    SchoolId = s.SchoolId,

                    ClassName = c != null ? c.ClassName : null,
                    SectionName = sd != null ? sd.SectionName : null,
                    AcademicSession = ac != null ? ac.Year_Start : (DateTime?)null
                }
            ).ToListAsync();

            return students;
        }

        public async Task<bool> AddStudentAsync(StudentCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Add Parent
                var parent = new ParentDetails
                {
                    Name = dto.Parent.Name,
                    PhoneNumber = dto.Parent.PhoneNumber,
                    Address = dto.Parent.Address,
                    Email = dto.Parent.Email,
                    Relationship = dto.Parent.Relationship,
                    Created_By = 1, // Replace with current user id
                    Updated_By = 1,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };

                _context.ParentDetails.Add(parent);
                await _context.SaveChangesAsync();

                // 2. Add Student
                var student = new Students
                {
                    StudentName = dto.StudentName,
                    DOB = dto.DOB,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    ParentId = parent.Id,
                    SchoolId = dto.SchoolId,
                    Created_By = 1,
                    Updated_By = 1,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };
              

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // 3. Add StudentEnrollment
                var enrollment = new StudentEnrollment
                {
                    StudentId = student.Id,
                    ClassId = dto.ClassId,
                    SectionId = dto.SectionId,
                    SessionId = dto.SessionId,
                    SchoolId = dto.SchoolId,
                    Created_By = 1,
                    Updated_By = 1,
                    Created_At = DateTime.Now,
                    IsActive = true
                };

                _context.StudentEnrollment.Add(enrollment);
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log error here
                return false;
            }
        }

        public async Task<List<EnrollmentInfoDto>> GetEnrollmentInfoBySchoolAsync(int schoolId)
        {
            var result = await (
                from se in _context.Schools
                join c in _context.Classes on se.Id equals c.SchoolId
                join sd in _context.SectionDetails on se.Id equals sd.SchoolId
                join s in _context.AcademicSessions on se.Id equals s.SchoolId
               // where se.SchoolId == schoolId
                select new EnrollmentInfoDto
                {
                    ClassId = c.Id,
                    ClassName = c.ClassName,
                    SectionId = sd.Id,
                    SectionName = sd.SectionName,
                    SessionId = s.Id,
                    YearStart = s.Year_Start,
                    YearEnd = s.Year_End
                }
            ).Distinct().ToListAsync();

            return result;
        }
    }
}
