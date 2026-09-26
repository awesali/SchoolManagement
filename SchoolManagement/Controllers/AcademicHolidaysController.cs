using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/AcademicHolidays")]
public class AcademicHolidaysController : ControllerBase
{
    private readonly AppDbContext _db;
    public AcademicHolidaysController(AppDbContext db) => _db = db;
    private bool CanManage(int schoolId) => User.FindFirstValue("RoleId") == "1" ||
        (User.FindFirstValue("RoleId") != "2" && int.TryParse(User.FindFirstValue("SchoolId"), out var claimed) && claimed == schoolId);

    [HttpGet("school/{schoolId:int}")]
    public async Task<IActionResult> List(int schoolId, int sessionId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var session = await _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
        if (session == null) return NotFound(new { message = "Academic session not found." });
        var rows = await _db.SchoolCalendarEvents.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive && x.EventType == "AcademicHoliday" &&
                x.EventDate.Date <= session.Year_End.Date && (x.EndDate ?? x.EventDate).Date >= session.Year_Start.Date)
            .OrderBy(x => x.EventDate)
            .Select(x => new { x.Id, x.Title, x.Description, x.EventDate, x.EndDate }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost]
    public async Task<IActionResult> Add(AcademicHolidayRequest request)
    {
        if (!CanManage(request.SchoolId)) return Forbid();
        var session = await _db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SessionId && x.SchoolId == request.SchoolId);
        if (session == null) return NotFound(new { message = "Academic session not found." });
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 || request.Description?.Length > 1000 ||
            request.FromDate == default || request.ToDate.Date < request.FromDate.Date ||
            request.FromDate.Date < session.Year_Start.Date || request.ToDate.Date > session.Year_End.Date)
            return BadRequest(new { message = "Enter a name and dates within the selected academic session." });
        if (await _db.SchoolCalendarEvents.AnyAsync(x => x.SchoolId == request.SchoolId && x.IsActive &&
            x.EventType == "AcademicHoliday" && x.EventDate.Date <= request.ToDate.Date &&
            (x.EndDate ?? x.EventDate).Date >= request.FromDate.Date))
            return Conflict(new { message = "An academic holiday already covers these dates." });
        var holiday = new SchoolCalendarEvent { SchoolId = request.SchoolId, SectionId = null,
            Title = request.Title.Trim(), Description = request.Description?.Trim(),
            EventDate = request.FromDate.Date, EndDate = request.ToDate.Date, EventType = "AcademicHoliday" };
        _db.SchoolCalendarEvents.Add(holiday);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { holiday.Id }, message = "Academic holiday added." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, int schoolId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var holiday = await _db.SchoolCalendarEvents.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId && x.EventType == "AcademicHoliday" && x.IsActive);
        if (holiday == null) return NotFound();
        holiday.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Academic holiday removed." });
    }
}
public record AcademicHolidayRequest(int SchoolId, int SessionId, string Title, string? Description, DateTime FromDate, DateTime ToDate);
