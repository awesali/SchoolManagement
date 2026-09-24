using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize]
public class SyllabusController : ControllerBase
{
    private readonly AppDbContext _db;
    public SyllabusController(AppDbContext db) => _db = db;

    private async Task<(int SchoolId, int? StaffId)?> Scope(bool teacher)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (user == null) return null;
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.RoleId && x.IsActive);
        if (teacher ? user.RoleId != 2 : !string.Equals(role?.RoleName?.Trim(), "Principal", StringComparison.OrdinalIgnoreCase)) return null;
        var staff = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == id && x.IsActive);
        var schoolId = teacher ? staff?.SchoolId : user.School_Id ?? staff?.SchoolId;
        if (schoolId == null || !await _db.Schools.AnyAsync(x => x.Id == schoolId && x.IsActive)) return null;
        return (schoolId.Value, teacher ? staff?.Id : null);
    }

    private IQueryable<AssignmentRow> Assignments(int schoolId, int? staffId)
    {
        return (from m in _db.SectionSubjectTeachers.AsNoTracking()
                join s in _db.SectionDetails.AsNoTracking() on m.SectionId equals s.Id
                join c in _db.Classes.AsNoTracking() on s.ClassId equals c.Id
                join sub in _db.Subjects.AsNoTracking() on m.SubjectId equals sub.Id
                where m.SchoolId == schoolId && s.SchoolId == schoolId && c.SchoolId == schoolId && sub.SchoolId == schoolId
                    && m.IsActive && s.IsActive && c.IsActive && sub.IsActive
                    && (staffId == null || m.StaffId == staffId)
                select new AssignmentRow { ClassId = c.Id, ClassName = c.ClassName, SectionId = s.Id,
                    SectionName = s.SectionName, SubjectId = sub.Id, SubjectName = sub.SubjectName }).Distinct();
    }

    [HttpGet("api/principal/syllabus")]
    [RequirePermission("academics.classes.read")]
    public Task<IActionResult> Principal(DateTime? date = null) => Read(false, date);

    [HttpGet("api/Teacher/syllabus")]
    public Task<IActionResult> Teacher(DateTime? date = null) => Read(true, date);

    private async Task<IActionResult> Read(bool teacher, DateTime? date)
    {
        var scope = await Scope(teacher);
        if (scope == null) return Forbid();
        var day = (date ?? DateTime.UtcNow.AddHours(5.5)).Date;
        var schoolId = scope.Value.SchoolId;
        var session = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive && x.Year_Start <= day && x.Year_End >= day)
            .OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        if (session == null) return Ok(new { academicYear = (string?)null, rows = Array.Empty<object>() });
        var assigned = await Assignments(schoolId, scope.Value.StaffId).ToListAsync();
        try
        {
            var records = await _db.SyllabusProgress.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.SessionId == session.Id && x.ProgressDate <= day)
                .ToListAsync();
            var latest = records.OrderByDescending(x => x.ProgressDate).ThenByDescending(x => x.Id)
                .GroupBy(x => (x.SectionId, x.SubjectId)).ToDictionary(x => x.Key, x => x.First());
            return Ok(new { academicYear = $"{session.Year_Start:yyyy}–{session.Year_End:yy}",
                rows = assigned.Select(a => {
                    latest.TryGetValue((a.SectionId, a.SubjectId), out var record);
                    return new { a.ClassId, a.ClassName, a.SectionId, a.SectionName, a.SubjectId, a.SubjectName,
                        totalChapters = record?.TotalChapters, plannedChapters = record?.PlannedChapters,
                        completedChapters = record?.CompletedChapters, progressDate = record?.ProgressDate.ToString("yyyy-MM-dd") };
                }) });
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return StatusCode(503, new { message = "Syllabus tracking is awaiting database setup. Please contact your administrator." });
        }
    }

    [HttpPost("api/Teacher/syllabus")]
    public async Task<IActionResult> Save(SyllabusUpdate request)
    {
        var scope = await Scope(true);
        if (scope == null || scope.Value.StaffId == null) return Forbid();
        var day = request.ProgressDate.Date;
        if (day > DateTime.UtcNow.AddHours(5.5).Date)
            return BadRequest(new { message = "Progress date cannot be in the future." });
        if (request.CompletedChapters > request.TotalChapters || request.PlannedChapters > request.TotalChapters)
            return BadRequest(new { message = "Planned and completed chapters cannot exceed total chapters." });
        if (!await Assignments(scope.Value.SchoolId, scope.Value.StaffId)
            .AnyAsync(x => x.SectionId == request.SectionId && x.SubjectId == request.SubjectId)) return Forbid();
        var session = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == scope.Value.SchoolId && x.IsActive && x.Year_Start <= day && x.Year_End >= day)
            .OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        if (session == null) return BadRequest(new { message = "No active academic year covers this date." });
        var row = new SyllabusProgress { SchoolId = scope.Value.SchoolId, StaffId = scope.Value.StaffId.Value,
            SessionId = session.Id, SectionId = request.SectionId, SubjectId = request.SubjectId, ProgressDate = day,
            TotalChapters = request.TotalChapters, PlannedChapters = request.PlannedChapters, CompletedChapters = request.CompletedChapters };
        _db.SyllabusProgress.Add(row);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 208 })
        { return StatusCode(503, new { message = "Syllabus tracking is awaiting database setup. Please contact your administrator." }); }
        return Ok(new { message = "Syllabus progress recorded.", row.Id });
    }

    private sealed class AssignmentRow
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
    }
    public sealed class SyllabusUpdate
    {
        [Range(1, int.MaxValue)] public int SectionId { get; set; }
        [Range(1, int.MaxValue)] public int SubjectId { get; set; }
        public DateTime ProgressDate { get; set; }
        [Range(1, 5000)] public int TotalChapters { get; set; }
        [Range(0, 5000)] public int PlannedChapters { get; set; }
        [Range(0, 5000)] public int CompletedChapters { get; set; }
    }
}
