using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/ca")]
public class CaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;
    public CaController(AppDbContext db, IPermissionService permissions) { _db = db; _permissions = permissions; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(DateTime? date = null)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null) return Unauthorized();
        var role = await _db.Roles.AsNoTracking().Where(x => x.Id == user.RoleId && x.IsActive).Select(x => x.RoleName).FirstOrDefaultAsync();
        if (!new[] { "ca", "accountant", "chartered accountant" }.Contains(role?.Trim().ToLowerInvariant())) return Forbid();
        var schoolId = user.School_Id ?? await _db.Staff.Where(x => x.usersid == userId && x.IsActive).Select(x => (int?)x.SchoolId).FirstOrDefaultAsync();
        var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(x => x.Id == schoolId && x.IsActive);
        if (school == null) return NotFound(new { message = "No active school is assigned to this account." });
        var today = DateTime.UtcNow.AddHours(5.5).Date;
        var day = (date ?? today).Date;
        if (day > today || day.Year < 1900) return BadRequest(new { message = "Choose a valid date up to today." });
        var end = day.AddDays(1);
        var month = new DateTime(day.Year, day.Month, 1);
        var session = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == school.Id && x.IsActive && x.Year_Start <= day && x.Year_End >= day).OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        var canFees = await _permissions.HasPermissionAsync(User, "finance.fees.read");
        var canSalary = await _permissions.HasPermissionAsync(User, "finance.salary.read");
        object? fees = null, payroll = null;
        if (canFees)
        {
            var payments = await (from p in _db.FeePayments.AsNoTracking()
                join f in _db.StudentFees on p.StudentFeeId equals f.Id
                join s in _db.Students on f.StudentId equals s.Id
                where p.SchoolId == school.Id && f.SchoolId == school.Id && s.SchoolId == school.Id && p.IsActive && f.IsActive && p.Payment_Date >= month && p.Payment_Date < end
                orderby p.Payment_Date descending, p.Id descending
                select new { p.Id, studentName = s.StudentName, amount = p.AmountPaid, date = p.Payment_Date, mode = p.Payment_Mode, receipt = p.Receipt_Number }).ToListAsync();
            var sessionId = session?.Id ?? 0;
            var balances = await (from f in _db.StudentFees.AsNoTracking()
                join s in _db.Students on f.StudentId equals s.Id
                where f.SchoolId == school.Id && s.SchoolId == school.Id && f.IsActive && f.SessionId == sessionId && (f.Created_Date == null || f.Created_Date < end)
                select new { f.Id, studentName = s.StudentName, feeType = f.FeeType.Name, amount = f.Amount,
                    paid = f.FeePayments.Where(p => p.IsActive && p.SchoolId == school.Id && p.Payment_Date < end).Sum(p => (decimal?)p.AmountPaid) ?? 0m }).ToListAsync();
            fees = new {
                today = payments.Where(p => p.date >= day).Sum(p => p.amount),
                month = payments.Sum(p => p.amount),
                assessed = balances.Sum(f => f.amount), collected = balances.Sum(f => f.paid),
                outstanding = balances.Sum(f => Math.Max(0m, f.amount - f.paid)),
                payments,
                balances = balances.Select(f => new { f.Id, f.studentName, f.feeType, f.amount, f.paid, balance = Math.Max(0m, f.amount - f.paid) }),
                modes = payments.GroupBy(p => p.mode).Select(g => new { name = g.Key, amount = g.Sum(p => p.amount) }),
                trend = payments.GroupBy(p => p.date.Date).OrderBy(g => g.Key).Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), amount = g.Sum(p => p.amount) })
            };
        }
        if (canSalary)
        {
            var salaries = await _db.SalaryPayment.AsNoTracking().Where(x => x.schoolId == school.Id && x.SalaryYear == day.Year && x.SalaryMonth == day.Month).ToListAsync();
            payroll = new { total = salaries.Sum(x => x.NetSalary), paid = salaries.Where(x => x.Status == "Paid").Sum(x => x.NetSalary),
                pending = salaries.Count(x => x.Status != "Paid"), count = salaries.Count };
        }
        return Ok(new { schoolId = school.Id, schoolName = school.SchoolName, academicYear = session == null ? null : $"{session.Year_Start:yyyy}-{session.Year_End:yy}",
            date = day.ToString("yyyy-MM-dd"), generatedAt = DateTime.UtcNow, fees, payroll });
    }
}

