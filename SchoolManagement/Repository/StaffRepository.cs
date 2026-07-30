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
            if (dto.BasicSalary <= 0)
            {
                return new
                {
                    Success = false,
                    Message = "Basic salary must be greater than zero."
                };
            }

            var staff = await _context.Staff
                .FirstOrDefaultAsync(x => x.Id == dto.StaffId && x.IsActive);

            if (staff == null)
            {
                return new
                {
                    Success = false,
                    Message = "Active employee not found."
                };
            }

            var salary = await _context.StaffSalaryStructure
                .FirstOrDefaultAsync(x =>
                    x.StaffId == dto.StaffId &&
                    x.IsActive);

            if (salary != null && !dto.IsUpdate)
            {
                return new
                {
                    Success = false,
                    AlreadyAssigned = true,
                    Message = "Salary is already assigned to this employee. Use Edit Salary to make changes."
                };
            }

            if (salary != null)
            {
                salary.IsActive = false;
            }

            var newSalary = new StaffSalaryStructure
            {
                StaffId = dto.StaffId,
                schoolId = staff.SchoolId,
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
                Message = salary == null
                    ? "Salary assigned successfully."
                    : "Salary updated successfully."
            };
        }

        public async Task<object> GetAssignedSalary(int staffId)
        {
            var salary = await _context.StaffSalaryStructure
                .AsNoTracking()
                .Where(x => x.StaffId == staffId && x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.StaffId,
                    x.BasicSalary,
                    x.SalaryType,
                    x.EffectiveFrom
                })
                .FirstOrDefaultAsync();

            return new
            {
                Success = true,
                IsAssigned = salary != null,
                Data = salary,
                Message = salary == null
                    ? "Salary has not been assigned."
                    : "Salary is already assigned."
            };
        }
        public async Task<object> GenerateMonthlySalary(int month, int year, int schoolId)
        {
            var salaries = await (
                from salary in _context.StaffSalaryStructure
                join staff in _context.Staff on salary.StaffId equals staff.Id
                where salary.IsActive &&
                      staff.IsActive &&
                      staff.SchoolId == schoolId
                select salary)
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
                    schoolId = schoolId,
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
                        $"StaffId {item.StaffId} - Salary already paid for {item.Month}/{item.Year}");
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
                var reference = string.IsNullOrWhiteSpace(item.PaymentReference)
                    ? null
                    : $"Payment reference: {item.PaymentReference.Trim()}";
                salary.Remarks = string.Join(
                    Environment.NewLine,
                    new[] { reference, item.Remarks?.Trim() }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
                salary.Status = "Paid";

                paidCount++;
            }

            await _context.SaveChangesAsync();

            return new
            {
                Success = failedRecords.Count == 0,
                PaidCount = paidCount,
                FailedCount = failedRecords.Count,
                FailedRecords = failedRecords,
                Message = $"{paidCount} Salary Paid Successfully"
            };
        }
        public async Task<object> GetPendingSalary(int schoolId)
        {
            return await (
                from salary in _context.SalaryPayment
                join staff in _context.Staff on salary.StaffId equals staff.Id
                join role in _context.Roles on staff.RoleId equals role.Id
                where salary.Status == "Pending" &&
                      staff.SchoolId == schoolId &&
                      staff.IsActive
                select new
                {
                    salary.Id,
                    salary.StaffId,
                    EmployeeNumber = EF.Property<int?>(staff, nameof(Staff.usersid)) ?? 0,
                    StaffName = staff.Name,
                    Department = role.RoleName,
                    salary.SalaryMonth,
                    salary.SalaryYear,
                    salary.BasicSalary,
                    salary.NetSalary,
                    salary.Status,
                    salary.CreatedDate
                })
                .OrderBy(x => x.StaffName)
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

        public async Task<object> GetSalaryHistoryByPeriod(int schoolId, int month, int year)
        {
            if (month < 1 || month > 12)
                return Array.Empty<object>();

            return await (
                from payment in _context.SalaryPayment.AsNoTracking()
                join staff in _context.Staff.AsNoTracking() on payment.StaffId equals staff.Id
                join role in _context.Roles.AsNoTracking() on staff.RoleId equals role.Id
                where staff.SchoolId == schoolId &&
                      payment.Status == "Paid" &&
                      payment.SalaryMonth == month &&
                      payment.SalaryYear == year
                orderby staff.Name
                select new
                {
                    payment.Id,
                    payment.StaffId,
                    EmployeeNumber = EF.Property<int?>(staff, nameof(Staff.usersid)) ?? 0,
                    StaffName = staff.Name,
                    Department = role.RoleName,
                    payment.SalaryMonth,
                    payment.SalaryYear,
                    payment.BasicSalary,
                    payment.Bonus,
                    payment.Deduction,
                    payment.NetSalary,
                    payment.Status,
                    payment.PaymentDate,
                    payment.PaymentMethod,
                    payment.Remarks
                }).ToListAsync();
        }
        public async Task<object> GetDashboard(int schoolId)
        {
            var totalStaff = await _context.Staff
                .CountAsync(x => x.SchoolId == schoolId && x.IsActive);

            // Use the staff record as the source of school ownership. Legacy salary
            // payments were created without SchoolId and otherwise disappear here.
            var schoolPayments =
                from payment in _context.SalaryPayment
                join staff in _context.Staff on payment.StaffId equals staff.Id
                where staff.SchoolId == schoolId && staff.IsActive
                select payment;

            var paidSalary = await schoolPayments
                .Where(x => x.Status == "Paid")
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0;

            var pendingSalary = await schoolPayments
                .Where(x => x.Status == "Pending")
                .SumAsync(x => (decimal?)x.NetSalary) ?? 0;

            var pendingEmployees = await schoolPayments
                .Where(x => x.Status == "Pending")
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
