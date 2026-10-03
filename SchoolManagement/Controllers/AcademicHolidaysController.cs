// Backend section: HTTP endpoints and request handling.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/AcademicHolidays")]
// Exposes academic holidays HTTP endpoints and handles their requests.
public class AcademicHolidaysController : ControllerBase
{
    // Dependencies and state used by this component.
    private readonly AppDbContext _db;

    // Creates the component with its required dependencies.
    public AcademicHolidaysController(AppDbContext db) => _db = db;

    private bool CanManage(int schoolId) =>
        User.FindFirstValue("RoleId") == "1"
        || (
            User.FindFirstValue("RoleId") != "2"
            && int.TryParse(User.FindFirstValue("SchoolId"), out var claimed)
            && claimed == schoolId
        );

    [HttpGet("today")]
    public async Task<IActionResult> Today(int? schoolId)
    {
        int resolvedSchoolId;
        if (User.IsInRole("Student"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId))
                return Forbid();
            resolvedSchoolId = await _db.Students_Parents_Creds.AsNoTracking()
                .Where(x => x.Id == credentialId && x.IsActive && x.Status == "Active" && x.RoleName == "Student")
                .Select(x => x.School_Id).FirstOrDefaultAsync();
        }
        else if (User.FindFirstValue("RoleId") == "1" && schoolId.HasValue)
            resolvedSchoolId = schoolId.Value;
        else if (!int.TryParse(User.FindFirstValue("SchoolId"), out resolvedSchoolId)
            || (schoolId.HasValue && schoolId.Value != resolvedSchoolId))
            return Forbid();

        if (resolvedSchoolId <= 0) return Forbid();
        var today = DateTime.Today;
        if (today.DayOfWeek == DayOfWeek.Sunday)
            return Ok(new { success = true, data = new { isHoliday = true, title = "Sunday holiday", date = today } });
        var holiday = await _db.SchoolCalendarEvents.AsNoTracking()
            .Where(x => x.SchoolId == resolvedSchoolId && x.IsActive && x.EventType == "AcademicHoliday"
                && x.EventDate < today.AddDays(1) && (x.EndDate ?? x.EventDate) >= today)
            .OrderBy(x => x.EventDate).Select(x => x.Title).FirstOrDefaultAsync();
        return Ok(new { success = true, data = new { isHoliday = holiday != null, title = holiday, date = today } });
    }

    [HttpGet("school/{schoolId:int}")]
    // API actions that validate requests and return responses.
    public async Task<IActionResult> List(int schoolId, int sessionId)
    {
        if (!CanManage(schoolId))
            return Forbid();
        var session = await _db
            .AcademicSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.SchoolId == schoolId);
        if (session == null)
            return NotFound(new { message = "Academic session not found." });
        var rows = await _db
            .SchoolCalendarEvents.AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId
                && x.IsActive
                && x.EventType == "AcademicHoliday"
                && x.EventDate.Date <= session.Year_End.Date
                && (x.EndDate ?? x.EventDate).Date >= session.Year_Start.Date
            )
            .OrderBy(x => x.EventDate)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.EventDate,
                x.EndDate,
            })
            .ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost]
    public async Task<IActionResult> Add(AcademicHolidayRequest request)
    {
        if (!CanManage(request.SchoolId))
            return Forbid();
        var session = await _db
            .AcademicSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SessionId && x.SchoolId == request.SchoolId);
        if (session == null)
            return NotFound(new { message = "Academic session not found." });
        if (
            string.IsNullOrWhiteSpace(request.Title)
            || request.Title.Trim().Length > 200
            || request.Description?.Length > 1000
            || request.FromDate == default
            || request.ToDate.Date < request.FromDate.Date
            || request.FromDate.Date < session.Year_Start.Date
            || request.ToDate.Date > session.Year_End.Date
        )
            return BadRequest(
                new { message = "Enter a name and dates within the selected academic session." }
            );
        if (
            await _db.SchoolCalendarEvents.AnyAsync(x =>
                x.SchoolId == request.SchoolId
                && x.IsActive
                && x.EventType == "AcademicHoliday"
                && x.EventDate.Date <= request.ToDate.Date
                && (x.EndDate ?? x.EventDate).Date >= request.FromDate.Date
            )
        )
            return Conflict(new { message = "An academic holiday already covers these dates." });
        var holiday = new SchoolCalendarEvent
        {
            SchoolId = request.SchoolId,
            SectionId = null,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.FromDate.Date,
            EndDate = request.ToDate.Date,
            EventType = "AcademicHoliday",
        };
        _db.SchoolCalendarEvents.Add(holiday);
        await _db.SaveChangesAsync();
        return Ok(
            new
            {
                success = true,
                data = new { holiday.Id },
                message = "Academic holiday added.",
            }
        );
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, int schoolId)
    {
        if (!CanManage(schoolId))
            return Forbid();
        var holiday = await _db.SchoolCalendarEvents.FirstOrDefaultAsync(x =>
            x.Id == id && x.SchoolId == schoolId && x.EventType == "AcademicHoliday" && x.IsActive
        );
        if (holiday == null)
            return NotFound();
        holiday.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Academic holiday removed." });
    }
}

public record AcademicHolidayRequest(
    int SchoolId,
    int SessionId,
    string Title,
    string? Description,
    DateTime FromDate,
    DateTime ToDate
);
