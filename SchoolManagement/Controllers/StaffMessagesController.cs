using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/StaffMessages")]
public class StaffMessagesController : ControllerBase
{
    private readonly AppDbContext _db;
    public StaffMessagesController(AppDbContext db) => _db = db;

    private async Task<Staff?> CurrentStaff()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !int.TryParse(User.FindFirstValue("RoleId"), out var roleId) || (roleId == 1 || roleId == 7)) return null;
        var account = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId &&
            x.IsActive && x.Status && x.RoleId == roleId);
        if (account == null) return null;
        return await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == userId &&
            x.RoleId == roleId && x.IsActive && x.SchoolId > 0);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var messages = await (from message in _db.TeacherStudentMessages.AsNoTracking()
            join student in _db.Students.AsNoTracking() on message.StudentId equals student.Id
            where message.StaffId == staff.Id && message.SchoolId == staff.SchoolId &&
                message.IsActive && student.SchoolId == staff.SchoolId
            orderby message.SentAt descending, message.Id descending
            select new { message.Id, message.StudentId, student.StudentName, message.Body,
                message.FromStudent, message.SentAt, message.ReadAt }).ToListAsync();
        var conversations = messages.GroupBy(x => x.StudentId).Select(group => {
            var latest = group.First();
            return new { studentId = group.Key, latest.StudentName, lastMessage = latest.Body,
                lastSentAt = latest.SentAt, lastFromStudent = latest.FromStudent,
                unreadCount = group.Count(x => x.FromStudent && x.ReadAt == null) };
        }).OrderByDescending(x => x.lastSentAt).ToList();
        return Ok(new { success = true, data = conversations });
    }

    [HttpGet("conversations/{studentId:int}")]
    public async Task<IActionResult> Thread(int studentId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await _db.TeacherStudentMessages.AnyAsync(x => x.StaffId == staff.Id &&
            x.SchoolId == staff.SchoolId && x.StudentId == studentId && x.IsActive)) return NotFound();
        var messages = await _db.TeacherStudentMessages.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId &&
                x.StudentId == studentId && x.IsActive)
            .OrderBy(x => x.SentAt).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Body, x.FromStudent, x.SentAt, x.ReadAt }).ToListAsync();
        return Ok(new { success = true, data = messages });
    }

    [HttpPost("conversations/{studentId:int}/read")]
    public async Task<IActionResult> Read(int studentId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        await _db.TeacherStudentMessages.Where(x => x.StaffId == staff.Id &&
            x.SchoolId == staff.SchoolId && x.StudentId == studentId && x.IsActive &&
            x.FromStudent && x.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, DateTime.UtcNow));
        return Ok(new { success = true });
    }
    [HttpPost("conversations/{studentId:int}")]
    public async Task<IActionResult> Reply(int studentId, [FromBody] StaffMessageInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000)
            return BadRequest(new { message = "Enter a message up to 2000 characters." });
        if (!await _db.TeacherStudentMessages.AnyAsync(x => x.StaffId == staff.Id &&
            x.SchoolId == staff.SchoolId && x.StudentId == studentId && x.IsActive)) return NotFound();
        _db.TeacherStudentMessages.Add(new TeacherStudentMessage {
            SchoolId = staff.SchoolId, StaffId = staff.Id, StudentId = studentId,
            Body = input.Body.Trim(), FromStudent = false
        });
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class StaffMessageInput { public string Body { get; set; } = ""; }