using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/SchoolStudentAdmin")]
public class SchoolStudentAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public SchoolStudentAdminController(AppDbContext db) => _db = db;
    private bool CanManage(int schoolId)
    {
        var roleId = User.FindFirstValue("RoleId");
        return roleId == "1" || (roleId != null && roleId != "2" &&
            int.TryParse(User.FindFirstValue("SchoolId"), out var claimedSchool) && claimedSchool == schoolId);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] int schoolId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var rows = await (from request in _db.StudentServiceRequests.AsNoTracking()
            join student in _db.Students on request.StudentId equals student.Id
            where request.SchoolId == schoolId && request.IsActive
            orderby request.CreatedAt descending
            select new { request.Id, student.StudentName, request.Type, request.Subject,
                request.Details, request.FromDate, request.ToDate, request.Status,
                request.Response, request.CreatedAt }).Take(300).ToListAsync();
        return Ok(new { success = true, data = rows });
    }
    [HttpPost("requests/{id:int}/respond")]
    public async Task<IActionResult> Respond(int id, [FromBody] StudentRequestReviewInput input)
    {
        var request = await _db.StudentServiceRequests.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (request == null) return NotFound();
        if (!CanManage(request.SchoolId)) return Forbid();
        if (!new[] { "Approved", "Rejected", "Resolved", "Pending" }.Contains(input.Status) ||
            input.Response?.Length > 2000) return BadRequest(new { message = "Choose a valid status and response." });
        request.Status = input.Status;
        request.Response = input.Response?.Trim();
        request.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
    [HttpPost("achievements")]
    public async Task<IActionResult> Achievement([FromBody] StudentAchievementInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 ||
            input.Description?.Length > 1000 || !await _db.Students.AnyAsync(x =>
                x.Id == input.StudentId && x.SchoolId == input.SchoolId && x.IsActive))
            return BadRequest(new { message = "Choose a student and enter a title." });
        var item = new StudentAchievement { SchoolId = input.SchoolId, StudentId = input.StudentId,
            Title = input.Title.Trim(), Description = input.Description?.Trim(),
            AwardedAt = input.AwardedAt == default ? DateTime.Today : input.AwardedAt.Date };
        _db.StudentAchievements.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpPost("events")]
    public async Task<IActionResult> Event([FromBody] SchoolEventInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 ||
            input.Description?.Length > 1000 || input.EventDate == default)
            return BadRequest(new { message = "Enter an event title and date." });
        if (input.SectionId.HasValue && !await _db.SectionDetails.AnyAsync(x =>
            x.Id == input.SectionId.Value && x.SchoolId == input.SchoolId && x.IsActive)) return BadRequest();
        var item = new SchoolCalendarEvent { SchoolId = input.SchoolId, SectionId = input.SectionId,
            Title = input.Title.Trim(), Description = input.Description?.Trim(), EventDate = input.EventDate };
        _db.SchoolCalendarEvents.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpPost("announcements")]
    public async Task<IActionResult> Announcement([FromBody] AdminAnnouncementInput input)
    {
        if (!CanManage(input.SchoolId) || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Body) ||
            input.Title.Length > 200 || input.Body.Length > 4000) return BadRequest(new { message = "Enter title and message." });
        if (input.SectionId.HasValue && !await _db.SectionDetails.AnyAsync(x =>
            x.Id == input.SectionId.Value && x.SchoolId == input.SchoolId && x.IsActive)) return BadRequest();
        var item = new SchoolAnnouncement { SchoolId = input.SchoolId, SectionId = input.SectionId,
            CreatedBy = userId, Title = input.Title.Trim(), Body = input.Body.Trim(),
            ExpiresAt = input.ExpiresAt, IsPinned = input.IsPinned, IsPublished = input.Publish };
        _db.SchoolAnnouncements.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpGet("exam-options")]
    public async Task<IActionResult> ExamOptions([FromQuery] int schoolId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var rows = await _db.Exams.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderByDescending(x => x.StartDate).Select(x => new { x.Id, x.Name }).Take(100).ToListAsync();
        return Ok(new { success = true, data = rows });
    }    [HttpGet("hall-tickets")]
    public async Task<IActionResult> HallTickets([FromQuery] int schoolId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var rows = await (from ticket in _db.StudentHallTickets.AsNoTracking()
            join student in _db.Students on ticket.StudentId equals student.Id
            join exam in _db.Exams on ticket.ExamId equals exam.Id
            where ticket.SchoolId == schoolId && ticket.IsActive
            select new { ticket.Id, ticket.StudentId, student.StudentName, ticket.ExamId,
                examName = exam.Name, ticket.SeatNumber, ticket.Room, ticket.DocumentUrl,
                ticket.IsPublished }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }
    [HttpPost("hall-tickets")]
    public async Task<IActionResult> SaveHallTicket([FromBody] HallTicketInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (!await _db.Students.AnyAsync(x => x.Id == input.StudentId && x.SchoolId == input.SchoolId && x.IsActive) ||
            !await _db.Exams.AnyAsync(x => x.Id == input.ExamId && x.SchoolId == input.SchoolId && x.IsActive) ||
            string.IsNullOrWhiteSpace(input.SeatNumber) || input.SeatNumber.Length > 50 ||
            string.IsNullOrWhiteSpace(input.Room) || input.Room.Length > 100)
            return BadRequest(new { message = "Choose a student, exam, seat number and room." });
        if (!string.IsNullOrWhiteSpace(input.DocumentUrl) &&
            (!Uri.TryCreate(input.DocumentUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return BadRequest(new { message = "Enter a valid document link." });
        var item = await _db.StudentHallTickets.FirstOrDefaultAsync(x => x.StudentId == input.StudentId &&
            x.ExamId == input.ExamId && x.SchoolId == input.SchoolId && x.IsActive);
        if (item == null) { item = new StudentHallTicket { SchoolId = input.SchoolId,
            StudentId = input.StudentId, ExamId = input.ExamId }; _db.StudentHallTickets.Add(item); }
        item.SeatNumber = input.SeatNumber.Trim(); item.Room = input.Room.Trim();
        item.DocumentUrl = input.DocumentUrl?.Trim(); item.IsPublished = input.Publish;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
}
public class StudentRequestReviewInput { public string Status { get; set; } = ""; public string? Response { get; set; } }
public class StudentAchievementInput { public int SchoolId { get; set; } public int StudentId { get; set; } public string Title { get; set; } = ""; public string? Description { get; set; } public DateTime AwardedAt { get; set; } }
public class SchoolEventInput { public int SchoolId { get; set; } public int? SectionId { get; set; } public string Title { get; set; } = ""; public string? Description { get; set; } public DateTime EventDate { get; set; } }
public class AdminAnnouncementInput { public int SchoolId { get; set; } public int? SectionId { get; set; } public string Title { get; set; } = ""; public string Body { get; set; } = ""; public DateTime? ExpiresAt { get; set; } public bool IsPinned { get; set; } public bool Publish { get; set; } = true; }


public class HallTicketInput { public int SchoolId { get; set; } public int StudentId { get; set; } public int ExamId { get; set; } public string SeatNumber { get; set; } = ""; public string Room { get; set; } = ""; public string? DocumentUrl { get; set; } public bool Publish { get; set; } = true; }


