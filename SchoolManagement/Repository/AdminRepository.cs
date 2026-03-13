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
    }
}
