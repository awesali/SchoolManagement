using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/Teacher")]
public class TeacherDiscussionController : ControllerBase
{
    private readonly AppDbContext _db;
    public TeacherDiscussionController(AppDbContext db) => _db = db;
    private async Task<Staff?> CurrentStaff()
    {
        if (User.FindFirstValue("RoleId") != "2" || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == userId && x.IsActive);
    }
    private Task<bool> CanTeach(Staff staff, int sectionId) => _db.SectionSubjectTeachers.AnyAsync(x => x.SchoolId == staff.SchoolId && x.StaffId == staff.Id && x.SectionId == sectionId && x.IsActive);

    [HttpGet("discussions")]
    public async Task<IActionResult> Threads()
    {
        var staff = await CurrentStaff(); if (staff == null) return Forbid();
        var sections = await _db.SectionSubjectTeachers.AsNoTracking().Where(x => x.SchoolId == staff.SchoolId && x.StaffId == staff.Id && x.IsActive).Select(x => x.SectionId).Distinct().ToListAsync();
        var threads = await _db.StudentDiscussionThreads.AsNoTracking().Where(x => x.SchoolId == staff.SchoolId && sections.Contains(x.SectionId) && x.IsActive)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.SectionId, x.Title, x.CreatedAt }).ToListAsync();
        return Ok(new { success = true, data = threads });
    }
    [HttpPost("discussions")]
    public async Task<IActionResult> CreateThread([FromBody] CreateThreadInput input)
    {
        var staff = await CurrentStaff(); if (staff == null) return Forbid();
        if (!await CanTeach(staff, input.SectionId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200) return BadRequest();
        var thread = new StudentDiscussionThread { SchoolId = staff.SchoolId, SectionId = input.SectionId, StaffId = staff.Id, Title = input.Title.Trim() };
        _db.StudentDiscussionThreads.Add(thread); await _db.SaveChangesAsync(); return Ok(new { success = true, data = new { thread.Id } });
    }
    [HttpGet("discussions/{id:int}/posts")]
    public async Task<IActionResult> Posts(int id)
    {
        var staff = await CurrentStaff(); if (staff == null) return Forbid();
        var thread = await _db.StudentDiscussionThreads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (thread == null || !await CanTeach(staff, thread.SectionId)) return Forbid();
        var posts = await _db.StudentDiscussionPosts.AsNoTracking().Where(x => x.SchoolId == staff.SchoolId && x.ThreadId == id && x.IsActive)
            .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.Body, x.StudentId, x.StaffId, x.IsApproved, x.CreatedAt }).ToListAsync();
        return Ok(new { success = true, data = posts });
    }
    [HttpPost("discussions/{id:int}/posts")]
    public async Task<IActionResult> Reply(int id, [FromBody] TeacherDiscussionInput input)
    {
        var staff = await CurrentStaff(); if (staff == null) return Forbid();
        var thread = await _db.StudentDiscussionThreads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (thread == null || !await CanTeach(staff, thread.SectionId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000) return BadRequest();
        _db.StudentDiscussionPosts.Add(new StudentDiscussionPost { SchoolId = staff.SchoolId, ThreadId = id, StaffId = staff.Id, Body = input.Body.Trim(), IsApproved = true });
        await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("discussions/posts/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var staff = await CurrentStaff(); if (staff == null) return Forbid();
        var post = await _db.StudentDiscussionPosts.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (post == null) return NotFound();
        var thread = await _db.StudentDiscussionThreads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == post.ThreadId && x.SchoolId == staff.SchoolId && x.IsActive);
        if (thread == null || !await CanTeach(staff, thread.SectionId)) return Forbid();
        post.IsApproved = true; await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
}
public class CreateThreadInput { public int SectionId { get; set; } public string Title { get; set; } = ""; }
public class TeacherDiscussionInput { public string Body { get; set; } = ""; }
