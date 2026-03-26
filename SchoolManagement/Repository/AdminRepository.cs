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
    }
}
