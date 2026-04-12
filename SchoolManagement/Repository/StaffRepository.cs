using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Repository
{
    public class StaffRepository : IStaffService
    {
        private readonly AppDbContext _context;
        private readonly ICommonRepository _common;
        private readonly IUserRepository _user;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public StaffRepository(AppDbContext context, IUserRepository user, ICommonRepository common, IWebHostEnvironment env, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _user = user;
            _common = common;
            _env = env;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto)
        {
            var staffId = int.Parse(_httpContextAccessor.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (staffId == 0)
                return new ApiResponse<string> { Success = false, Message = "Unauthorized" };

            var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId);
            if (staff == null)
                return new ApiResponse<string> { Success = false, Message = "Staff not found" };

            var schoolId = staff.School_Id;

            // ✅ Check if already marked
            var alreadyMarked = await _context.StaffAttendance
                .AnyAsync(a =>
                    a.Staff_Id == staffId &&
                    a.Attendance_Date.Date == dto.AttendanceDate.Date &&
                    a.School_Id == schoolId);

            if (alreadyMarked)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Attendance already marked for this date"
                };
            }

            // ✅ Insert
            var attendance = new StaffAttendance
            {
                Staff_Id = staffId,
                Attendance_Date = dto.AttendanceDate,
                Status = dto.Status,
               // School_Id = schoolId,
                Created_At = DateTime.Now,
                Created_By = staffId,
                IsActive = true
            };

            _context.StaffAttendance.Add(attendance);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Attendance marked successfully"
            };
        }

        public async Task<List<StaffAttendanceHistoryDto>> GetStaffAttendanceHistoryAsync(DateTime fromDate, DateTime toDate)
        {
            var staffId = int.Parse(_httpContextAccessor.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (staffId == 0)
                throw new Exception("Unauthorized");

            var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId);
            if (staff == null)
                throw new Exception("Staff not found");

            var schoolId = staff.School_Id;

            var history = await _context.StaffAttendance
                .Where(a =>
                    a.Staff_Id == staffId &&
                    a.School_Id == schoolId &&
                    a.Attendance_Date.Date >= fromDate.Date &&
                    a.Attendance_Date.Date <= toDate.Date)
                .OrderByDescending(a => a.Attendance_Date)
                .Select(a => new StaffAttendanceHistoryDto
                {
                    AttendanceDate = a.Attendance_Date,
                    Status = a.Status
                })
                .ToListAsync();

            return history;
        }
    }
}
