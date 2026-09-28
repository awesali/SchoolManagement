using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/receptionist")]
public class ReceptionistController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReceptionistController(AppDbContext db) { _db = db; }

    // Every action resolves the school from an active account; no client school ID is accepted.
    private async Task<(int SchoolId, int UserId)?> Access()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (user == null) return null;
        var role = await _db.Roles.AsNoTracking().Where(x => x.Id == user.RoleId && x.IsActive).Select(x => x.RoleName).FirstOrDefaultAsync();
        if (!new[] { "receptionist", "reception", "front desk", "front office" }.Contains(role?.Trim().ToLowerInvariant())) return null;
        var schoolId = user.School_Id ?? await _db.Staff.Where(x => x.usersid == id && x.IsActive).Select(x => (int?)x.SchoolId).FirstOrDefaultAsync();
        if (schoolId == null || !await _db.Schools.AnyAsync(x => x.Id == schoolId && x.IsActive)) return null;
        return (schoolId.Value, id);
    }

    private static DateTime SchoolNow() => DateTime.UtcNow.AddHours(5.5);
    private static bool Open(string status) => status is "Checked in" or "Open" or "Scheduled" or "Follow-up";
    private static readonly Dictionary<string, string[]> Statuses = new()
    {
        ["Visitor"] = new[] { "Checked in", "Checked out" },
        ["Enquiry"] = new[] { "Open", "Follow-up", "Resolved", "Closed" },
        ["Appointment"] = new[] { "Scheduled", "Completed", "Cancelled" },
        ["Call"] = new[] { "Open", "Follow-up", "Resolved" }
    };

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(DateTime? date = null)
    {
        var access = await Access(); if (access == null) return Forbid();
        var day = (date ?? SchoolNow()).Date;
        if (day.Year < 2000 || day > SchoolNow().Date.AddYears(1)) return BadRequest(new { message = "Choose a valid date." });
        var schoolId = access.Value.SchoolId;
        var end = day.AddDays(1);
        var query = _db.ReceptionEntries.AsNoTracking().Where(x => x.SchoolId == schoolId);
        var entries = await query.Where(x => x.ScheduledAt >= day && x.ScheduledAt < end).OrderByDescending(x => x.ScheduledAt).ThenByDescending(x => x.Id).ToListAsync();
        var followUps = await query.Where(x => x.FollowUpDate != null && x.FollowUpDate < end && (x.Status == "Open" || x.Status == "Follow-up" || x.Status == "Scheduled")).OrderBy(x => x.FollowUpDate).ThenBy(x => x.Id).ToListAsync();
        var onSite = await query.Where(x => x.Kind == "Visitor" && x.Status == "Checked in").OrderBy(x => x.ScheduledAt).ToListAsync();
        var school = await _db.Schools.Where(x => x.Id == schoolId).Select(x => x.SchoolName).FirstAsync();
        var session = await _db.AcademicSessions.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive && x.Year_Start <= day && x.Year_End >= day).OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        return Ok(new { schoolId, schoolName = school, academicYear = session == null ? null : $"{session.Year_Start:yyyy}-{session.Year_End:yy}", entries, followUps, onSite, generatedAt = DateTime.UtcNow });
    }

    public class EntryRequest
    {
        [Required, MaxLength(20)] public string Kind { get; set; } = "";
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        [MaxLength(30), RegularExpression(@"^[0-9+()\s-]*$")] public string Phone { get; set; } = "";
        [MaxLength(150)] public string ContactPerson { get; set; } = "";
        [Required, MaxLength(1000)] public string Purpose { get; set; } = "";
        public DateTime? ScheduledAt { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }

    [HttpPost("entries")]
    public async Task<IActionResult> Create(EntryRequest request)
    {
        var access = await Access(); if (access == null) return Forbid();
        if (!Statuses.ContainsKey(request.Kind) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Purpose))
            return BadRequest(new { message = "Choose a register and enter a name and purpose." });
        var now = SchoolNow();
        if (request.Kind == "Appointment" && (request.ScheduledAt == null || request.ScheduledAt < now || request.ScheduledAt > now.AddYears(1)))
            return BadRequest(new { message = "Choose an appointment time in the next year." });
        if (request.FollowUpDate != null && (request.FollowUpDate.Value.Date < now.Date || request.FollowUpDate > now.AddYears(1)))
            return BadRequest(new { message = "Choose a follow-up date from today through the next year." });
        var item = new ReceptionEntry {
            SchoolId = access.Value.SchoolId, Kind = request.Kind, Name = request.Name.Trim(), Phone = request.Phone?.Trim() ?? "",
            ContactPerson = request.ContactPerson?.Trim() ?? "", Purpose = request.Purpose.Trim(),
            Status = Statuses[request.Kind][0], ScheduledAt = request.Kind == "Appointment" ? request.ScheduledAt!.Value : now,
            FollowUpDate = request.FollowUpDate?.Date, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            CreatedBy = access.Value.UserId, UpdatedBy = access.Value.UserId
        };
        _db.ReceptionEntries.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    public class StatusRequest
    {
        [Required, MaxLength(30)] public string Status { get; set; } = "";
        [Required] public byte[] Version { get; set; } = Array.Empty<byte>();
        public DateTime? FollowUpDate { get; set; }
    }

    [HttpPut("entries/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, StatusRequest request)
    {
        var access = await Access(); if (access == null) return Forbid();
        var item = await _db.ReceptionEntries.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == access.Value.SchoolId);
        if (item == null) return NotFound(new { message = "The record was not found." });
        if (!Statuses.TryGetValue(item.Kind, out var allowed) || !allowed.Contains(request.Status) || !Open(item.Status) || request.Status == item.Status && request.FollowUpDate == item.FollowUpDate)
            return BadRequest(new { message = "This status change is not available." });
        if (request.Status == "Follow-up" && request.FollowUpDate == null)
            return BadRequest(new { message = "Choose a follow-up date." });
        if (request.FollowUpDate != null && (request.FollowUpDate.Value.Date < SchoolNow().Date || request.FollowUpDate > SchoolNow().AddYears(1)))
            return BadRequest(new { message = "Choose a follow-up date from today through the next year." });
        _db.Entry(item).Property(x => x.Version).OriginalValue = request.Version;
        item.Status = request.Status;
        item.FollowUpDate = Open(request.Status) ? request.FollowUpDate?.Date : null;
        item.UpdatedAt = DateTime.UtcNow; item.UpdatedBy = access.Value.UserId;
        if (request.Status == "Checked out") item.CheckedOutAt = SchoolNow();
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "Another receptionist updated this record. Refresh and try again." }); }
        return Ok(item);
    }
}
