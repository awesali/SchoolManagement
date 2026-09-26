using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;
[ApiController, Authorize, Route("api/StaffLeaveAllocations")]
public class StaffLeaveAllocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StaffLeaveAllocationsController(AppDbContext db) => _db = db;
    public static readonly string[] Types = { "Casual Leave", "Sick Leave", "Planned Leave", "Unpaid Leave", "Earned Leave" };
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<bool> IsAdmin(int schoolId) => await _db.Users.AnyAsync(x => x.Id == UserId && x.IsActive &&
        (x.RoleId == 1 || x.School_Id == schoolId && x.RoleId != 2));
    private async Task<AcademicSessions?> Session(int schoolId, int sessionId) =>
        await _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
    private async Task<object> Balance(int schoolId, int staffId, AcademicSessions session)
    {
        var allocations = await _db.StaffLeaveAllocations.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.StaffId == staffId && x.AcademicSessionId == session.Id).ToListAsync();
        var requests = await _db.StaffLeaveRequests.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.StaffId == staffId && x.IsActive && x.Status != "Rejected" &&
                x.FromDate.Date >= session.Year_Start.Date && x.ToDate.Date <= session.Year_End.Date)
            .Select(x => new { x.LeaveType, x.FromDate, x.ToDate, x.Status }).ToListAsync();
        return new { sessionId = session.Id, sessionStart = session.Year_Start, sessionEnd = session.Year_End,
            balances = Types.Select(type => { var allotted = allocations.FirstOrDefault(x => x.LeaveType == type)?.Days ?? 0;
                var used = requests.Where(x => x.LeaveType == type && x.Status == "Approved").Sum(x => (x.ToDate.Date - x.FromDate.Date).Days + 1);
                var pending = requests.Where(x => x.LeaveType == type && x.Status == "Pending").Sum(x => (x.ToDate.Date - x.FromDate.Date).Days + 1);
                return new { leaveType = type, allotted, used, pending, remaining = Math.Max(0, allotted - used - pending) }; }).ToList() };
    }
    [HttpGet("staff/{staffId:int}")]
    public async Task<IActionResult> StaffBalance(int staffId, int schoolId, int sessionId)
    {
        if (!await IsAdmin(schoolId) || !await _db.Staff.AnyAsync(x => x.Id == staffId && x.SchoolId == schoolId)) return Forbid();
        var session = await Session(schoolId, sessionId);
        return session == null ? NotFound() : Ok(new { success = true, data = await Balance(schoolId, staffId, session) });
    }
    [HttpGet("mine")]
    public async Task<IActionResult> MyBalance()
    {
        var staff = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == UserId && x.IsActive);
        if (staff == null) return Forbid();
        var today = DateTime.Today;
        var session = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == staff.SchoolId &&
            x.IsActive && x.Year_Start.Date <= today && x.Year_End.Date >= today)
            .OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        return session == null ? Ok(new { success = true, data = (object?)null }) :
            Ok(new { success = true, data = await Balance(staff.SchoolId, staff.Id, session) });
    }
    [HttpPut("staff/{staffId:int}")]
    public async Task<IActionResult> SetAllocation(int staffId, LeaveAllocationRequest request)
    {
        if (!await IsAdmin(request.SchoolId)) return Forbid();
        if (request.Days < 0 || request.Days > 366 || !Types.Contains(request.LeaveType)) return BadRequest(new { message = "Choose a valid leave type and allowance from 0 to 366 days." });
        if (!await _db.Staff.AnyAsync(x => x.Id == staffId && x.SchoolId == request.SchoolId) ||
            await Session(request.SchoolId, request.SessionId) == null) return NotFound();
        var row = await _db.StaffLeaveAllocations.FirstOrDefaultAsync(x => x.StaffId == staffId && x.AcademicSessionId == request.SessionId && x.LeaveType == request.LeaveType);
        if (row == null) { row = new StaffLeaveAllocation { SchoolId = request.SchoolId, StaffId = staffId, AcademicSessionId = request.SessionId, LeaveType = request.LeaveType }; _db.StaffLeaveAllocations.Add(row); }
        row.Days = request.Days; row.UpdatedAt = DateTime.UtcNow; row.UpdatedBy = UserId;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Leave allowance saved." });
    }
    [HttpPut("staff/{staffId:int}/all")]
    public async Task<IActionResult> SetAllAllocations(int staffId, LeaveAllocationsRequest request)
    {
        if (!await IsAdmin(request.SchoolId)) return Forbid();
        if (request.Allowances == null || request.Allowances.Count != Types.Length ||
            request.Allowances.Select(x => x.LeaveType).Distinct().Count() != Types.Length ||
            request.Allowances.Any(x => !Types.Contains(x.LeaveType) || x.Days < 0 || x.Days > 366))
            return BadRequest(new { message = "Enter a valid allowance between 0 and 366 for every leave type." });
        if (!await _db.Staff.AnyAsync(x => x.Id == staffId && x.SchoolId == request.SchoolId) ||
            await Session(request.SchoolId, request.SessionId) == null) return NotFound();
        var rows = await _db.StaffLeaveAllocations.Where(x => x.SchoolId == request.SchoolId &&
            x.StaffId == staffId && x.AcademicSessionId == request.SessionId).ToListAsync();
        foreach (var allowance in request.Allowances)
        {
            var row = rows.FirstOrDefault(x => x.LeaveType == allowance.LeaveType);
            if (row == null)
            {
                row = new StaffLeaveAllocation { SchoolId = request.SchoolId, StaffId = staffId,
                    AcademicSessionId = request.SessionId, LeaveType = allowance.LeaveType };
                _db.StaffLeaveAllocations.Add(row);
            }
            row.Days = allowance.Days; row.UpdatedAt = DateTime.UtcNow; row.UpdatedBy = UserId;
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Leave allowances saved." });
    }
}
public record LeaveAllocationRequest(int SchoolId, int SessionId, string LeaveType, int Days);
public record LeaveAllowancesRequestItem(string LeaveType, int Days);
public record LeaveAllocationsRequest(int SchoolId, int SessionId, List<LeaveAllowancesRequestItem> Allowances);
