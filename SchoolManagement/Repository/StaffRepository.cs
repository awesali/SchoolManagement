using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Repository
{
    public class StaffRepository : IStaffRepository
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
        //public async Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto)
        //{
        //    var staffId = int.Parse(_httpContextAccessor.HttpContext.User
        //        .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        //    if (staffId == 0)
        //        return new ApiResponse<string> { Success = false, Message = "Unauthorized" };

        //    var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId);
        //    if (staff == null)
        //        return new ApiResponse<string> { Success = false, Message = "Staff not found" };

        //    var schoolId = staff.School_Id;

        //    // ✅ Check if already marked
        //    var alreadyMarked = await _context.StaffAttendance
        //        .AnyAsync(a =>
        //            a.Staff_Id == staffId &&
        //            a.Attendance_Date.Date == dto.AttendanceDate.Date &&
        //            a.School_Id == schoolId);

        //    if (alreadyMarked)
        //    {
        //        return new ApiResponse<string>
        //        {
        //            Success = false,
        //            Message = "Attendance already marked for this date"
        //        };
        //    }

        //    // ✅ Insert
        //    var attendance = new StaffAttendance
        //    {
        //        Staff_Id = staffId,
        //        Attendance_Date = dto.AttendanceDate,
        //        Status = dto.Status,
        //        School_Id = schoolId,
        //        Created_At = DateTime.Now,
        //        Created_By = staffId,
        //        IsActive = true
        //    };

        //    _context.StaffAttendance.Add(attendance);
        //    await _context.SaveChangesAsync();

        //    return new ApiResponse<string>
        //    {
        //        Success = true,
        //        Message = "Attendance marked successfully"
        //    };
        //}

        public async Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto)
        {
            // ✅ Step 0: Get staffId from claims
            var staffIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;



            if (string.IsNullOrEmpty(staffIdClaim))
                return new ApiResponse<string> { Success = false, Message = "Unauthorized" };

            int staffId = int.Parse(staffIdClaim);

            // ✅ Step 1: Validate staff
            var staff = await _context.Staff.FirstOrDefaultAsync(u => u.Id == staffId);
            if (staff == null)
                return new ApiResponse<string> { Success = false, Message = "Staff not found" };

            var schoolId = staff.SchoolId;

            // ✅ Step 2: Normalize date (important 🔥)
            var attendanceDate = dto.AttendanceDate.Date;

            // ✅ Step 3: Strong check (same day restriction)
            var alreadyMarked = await _context.StaffAttendance
                .AnyAsync(a =>
                    a.Staff_Id == staff.Id &&
                    a.School_Id == schoolId &&
                    a.Attendance_Date >= attendanceDate &&
                    a.Attendance_Date < attendanceDate.AddDays(1)
                );

            if (alreadyMarked)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Attendance already marked for this date"
                };
            }

            // ✅ Step 4: Insert
            var attendance = new StaffAttendance
            {
                Staff_Id = staff.Id,
                Attendance_Date = attendanceDate,
                Status = dto.Status,
                School_Id = schoolId,
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

        public async Task<List<StaffAttendanceHistoryDto>> GetStaffAttendanceHistoryAsync(DateTime fromDate, DateTime toDate,int schoolid )
        {
            var staffId = int.Parse(_httpContextAccessor.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (staffId == 0)
                throw new Exception("Unauthorized");

            var history = await _context.StaffAttendance
                .Where(a =>
                    a.School_Id == schoolid &&
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

        public async Task<StaffAttendanceNotificationDto> CheckTodayAttendanceAsync()
        {
            var staffIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(staffIdClaim))
            {
                return new StaffAttendanceNotificationDto
                {
                    ShouldMarkAttendance = false,
                    Message = "Unauthorized"
                };
            }

            int staffId = int.Parse(staffIdClaim);

            var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId);
            if (staff == null)
            {
                return new StaffAttendanceNotificationDto
                {
                    ShouldMarkAttendance = false,
                    Message = "Staff not found"
                };
            }

            var schoolId = staff.School_Id;

            var today = DateTime.Today;

            var alreadyMarked = await _context.StaffAttendance
                .AnyAsync(a =>
                    a.Staff_Id == staffId &&
                    a.School_Id == schoolId &&
                    a.Attendance_Date >= today &&
                    a.Attendance_Date < today.AddDays(1)
                );

            if (alreadyMarked)
            {
                return new StaffAttendanceNotificationDto
                {
                    ShouldMarkAttendance = false,
                    Message = "Attendance already marked"
                };
            }

            return new StaffAttendanceNotificationDto
            {
                ShouldMarkAttendance = true,
                Message = "Please mark your attendance for today"
            };
        }
        public async Task<object> AssignSalary(AssignSalaryDto dto)
        {
            var salary = await _context.StaffSalaryStructure
                .FirstOrDefaultAsync(x =>
                    x.StaffId == dto.StaffId &&
                    x.IsActive);

            if (salary != null)
            {
                salary.IsActive = false;
            }

            var newSalary = new StaffSalaryStructure
            {
                StaffId = dto.StaffId,
                BasicSalary = dto.BasicSalary,
                SalaryType = dto.SalaryType,
                EffectiveFrom = DateTime.Now.Date,
                IsActive = true
            };

            _context.StaffSalaryStructure.Add(newSalary);

            await _context.SaveChangesAsync();

            return new
            {
                Success = true,
                Message = "Salary Assigned Successfully"
            };
        }
        public async Task<object> GenerateMonthlySalary(int month, int year)
        {
            var salaries = await _context.StaffSalaryStructure
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var salary in salaries)
            {
                bool exists = await _context.SalaryPayment
                    .AnyAsync(x =>
                        x.StaffId == salary.StaffId &&
                        x.SalaryMonth == month &&
                        x.SalaryYear == year);

                if (exists)
                    continue;

                _context.SalaryPayment.Add(new SalaryPayment
                {
                    StaffId = salary.StaffId,
                    SalaryMonth = month,
                    SalaryYear = year,
                    BasicSalary = salary.BasicSalary,
                    NetSalary = salary.BasicSalary,
                    Status = "Pending"
                });
            }

            await _context.SaveChangesAsync();

            return new
            {
                Success = true,
                Message = "Salary Generated Successfully"
            };
        }
        public async Task<object> PaySalary(PaySalaryDto dto)
        {
            var paidCount = 0;
            var failedRecords = new List<string>();

            foreach (var item in dto.Salaries)
            {
                var salary = await _context.SalaryPayment
                    .FirstOrDefaultAsync(x =>
                        x.StaffId == item.StaffId &&
                        x.SalaryMonth == item.Month &&
                        x.SalaryYear == item.Year);

                if (salary == null)
                {
                    failedRecords.Add(
                        $"StaffId {item.StaffId} - Salary Record Not Found");
                    continue;
                }

                if (salary.Status == "Paid")
                {
                    failedRecords.Add(
                        $"StaffId {item.StaffId} - Salary Already Paid");
                    continue;
                }

                salary.Bonus = item.Bonus;
                salary.Deduction = item.Deduction;

                salary.NetSalary =
                    salary.BasicSalary +
                    item.Bonus -
                    item.Deduction;

                salary.PaymentMethod = item.PaymentMethod;
                salary.PaymentDate = DateTime.Now;
                salary.Remarks = item.Remarks;
                salary.Status = "Paid";

                paidCount++;
            }

            await _context.SaveChangesAsync();

            return new
            {
                Success = true,
                PaidCount = paidCount,
                FailedCount = failedRecords.Count,
                FailedRecords = failedRecords,
                Message = $"{paidCount} Salary Paid Successfully"
            };
        }
        public async Task<object> GetPendingSalary()
        {
            return await _context.SalaryPayment
                .Include(x => x.Staff)
                .Where(x => x.Status == "Pending")
                .Select(x => new
                {
                    x.Id,
                    x.StaffId,
                    StaffName = x.Staff.Name,
                    x.SalaryMonth,
                    x.SalaryYear,
                    x.NetSalary
                })
                .ToListAsync();
        }
        public async Task<object> GetPendingSalaryByStaff(int staffId)
        {
            return await _context.SalaryPayment
                .Where(x =>
                    x.StaffId == staffId &&
                    x.Status == "Pending")
                .ToListAsync();
        }
        public async Task<object> GetSalaryHistory(int staffId)
        {
            return await _context.SalaryPayment
                .Where(x =>
                    x.StaffId == staffId &&
                    x.Status == "Paid")
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();
        }
        public async Task<object> GetDashboard(int schoolId)
        {
            var totalStaff = await _context.Staff
                .CountAsync(x => x.SchoolId == schoolId);

            var paidSalary =
                await _context.SalaryPayment
                .Where(x => x.Status == "Paid" && x.schoolId == schoolId)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0;

            var pendingSalary =
                await _context.SalaryPayment
                .Where(x => x.Status == "Pending" && x.schoolId == schoolId)
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0;

            var pendingEmployees =
                await _context.SalaryPayment
                .Where(x => x.Status == "Pending" && x.schoolId == schoolId)
                .Select(x => x.StaffId)
                .Distinct()
                .CountAsync();

            return new
            {
                TotalStaff = totalStaff,
                PaidSalary = paidSalary,
                PendingSalary = pendingSalary,
                PendingEmployees = pendingEmployees
            };
        }
    }
}
