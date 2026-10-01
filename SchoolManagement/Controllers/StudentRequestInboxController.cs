// Backend section: HTTP endpoints and request handling.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Service;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/StudentRequestInbox")]
// Exposes student request inbox HTTP endpoints and handles their requests.
public class StudentRequestInboxController : ControllerBase
{
    // Dependencies and state used by this component.
    private readonly AppDbContext _db;
    private readonly IEventEmailService _notifications;

    // Creates the component with its required dependencies.
    public StudentRequestInboxController(AppDbContext db, IEventEmailService notifications) { _db = db; _notifications = notifications; }

    private async Task<(int userId, int roleId, int schoolId)?> Recipient()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return null;
        var account = await _db
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Status);
        if (
            account?.RoleId == null
            || account.RoleId == 0
            || User.FindFirstValue("RoleId") != account.RoleId.Value.ToString()
        )
            return null;
        var staff = await _db
            .Staff.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.usersid == userId && x.IsActive && x.RoleId == account.RoleId
            );
        var schoolId = staff?.SchoolId ?? account.School_Id ?? 0;
        if (schoolId <= 0 && account.RoleId != 1)
            return null;
        return (userId, account.RoleId.Value, schoolId);
    }

    [HttpGet]
    // API actions that validate requests and return responses.
    public async Task<IActionResult> List()
    {
        var recipient = await Recipient();
        if (recipient == null)
            return Forbid();
        var (userId, roleId, schoolId) = recipient.Value;
        var rows = await (
            from request in _db.StudentServiceRequests.AsNoTracking()
            join student in _db.Students.AsNoTracking() on request.StudentId equals student.Id
            join enrollment in _db.StudentEnrollment.AsNoTracking()
                on request.EnrollmentId equals enrollment.Id
            join section in _db.SectionDetails.AsNoTracking()
                on enrollment.SectionId equals section.Id
            join classroom in _db.Classes.AsNoTracking() on enrollment.ClassId equals classroom.Id
            where
                request.IsActive
                && request.RecipientUserId == userId
                && request.RecipientRoleId == roleId
                && (roleId == 1 || request.SchoolId == schoolId)
                && student.SchoolId == request.SchoolId
            orderby request.CreatedAt descending
            select new
            {
                request.Id,
                student.StudentName,
                classroom.ClassName,
                section.SectionName,
                request.Type,
                request.Subject,
                request.Details,
                request.FromDate,
                request.ToDate,
                request.Status,
                request.Response,
                request.CreatedAt,
            }
        )
            .Take(300)
            .ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("{id:int}/respond")]
    public async Task<IActionResult> Respond(
        int id,
        [FromBody] StudentRequestInboxReviewInput input
    )
    {
        var recipient = await Recipient();
        if (recipient == null)
            return Forbid();
        var (userId, roleId, schoolId) = recipient.Value;
        var request = await _db.StudentServiceRequests.FirstOrDefaultAsync(x =>
            x.Id == id
            && x.IsActive
            && x.RecipientUserId == userId
            && x.RecipientRoleId == roleId
            && (roleId == 1 || x.SchoolId == schoolId)
        );
        if (request == null)
            return NotFound();
        if (
            !new[] { "Approved", "Rejected", "Resolved", "Pending" }.Contains(input.Status)
            || input.Response?.Length > 2000
        )
            return BadRequest(new { message = "Choose a valid status and response." });
        if (request.Type == "Leave" && request.Status != input.Status && (input.Status == "Approved" || input.Status == "Rejected"))
        {
            try { await _notifications.SendToStudentAsync(request.SchoolId, request.StudentId,
                "LEAVE_REQUEST_DECIDED", new() {
                    ["Decision"] = input.Status.ToLowerInvariant(),
                    ["FromDate"] = request.FromDate?.ToString("dd MMM yyyy") ?? "",
                    ["ToDate"] = request.ToDate?.ToString("dd MMM yyyy") ?? "",
                    ["Response"] = input.Response?.Trim() ?? "No additional response" }); }
            catch (Exception) { return StatusCode(503, new { message = "Leave decision email could not be sent. Nothing was saved." }); }
        }
        request.Status = input.Status;
        request.Response = input.Response?.Trim();
        request.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class StudentRequestInboxReviewInput
{
    public string Status { get; set; } = "";
    public string? Response { get; set; }
}
