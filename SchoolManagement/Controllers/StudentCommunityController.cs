using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/StudentCommunity")]
public class StudentCommunityController : ControllerBase
{
    private readonly AppDbContext _db;
    public StudentCommunityController(AppDbContext db) => _db = db;

    private async Task<(Students? student, StudentEnrollment? enrollment, int grade)> Current()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)) return (null, null, 0);
        var credential = await _db.Students_Parents_Creds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == credentialId && x.IsActive && x.Status == "Active" && x.RoleName == "Student");
        if (credential == null) return (null, null, 0);
        var matches = await _db.Students.AsNoTracking().Where(x => x.SchoolId == credential.School_Id && x.Email == credential.Email && x.IsActive).Take(2).ToListAsync();
        if (matches.Count != 1) return (null, null, 0);
        var student = matches[0];
        var enrollment = await _db.StudentEnrollment.AsNoTracking().Where(x => x.StudentId == student.Id && x.SchoolId == student.SchoolId && x.IsActive && x.EnrollmentStatus == "Active").OrderByDescending(x => x.EnrollmentDate).FirstOrDefaultAsync();
        if (enrollment == null) return (student, null, 0);
        var className = await _db.Classes.AsNoTracking().Where(x => x.Id == enrollment.ClassId && x.SchoolId == student.SchoolId).Select(x => x.ClassName).FirstOrDefaultAsync();
        var match = Regex.Match(className ?? "", @"\d+");
        return (student, enrollment, match.Success && int.TryParse(match.Value, out var grade) ? grade : 0);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var (student, enrollment, grade) = await Current();
        if (student == null || enrollment == null) return Forbid();
        var classmates = await (from e in _db.StudentEnrollment.AsNoTracking()
            join s in _db.Students on e.StudentId equals s.Id
            where e.SchoolId == student.SchoolId && e.SessionId == enrollment.SessionId && e.SectionId == enrollment.SectionId &&
                e.IsActive && e.EnrollmentStatus == "Active" && s.IsActive
            orderby s.StudentName
            select new { s.StudentName, rollNumber = e.RollNumber ?? s.Rollnumber }).Take(100).ToListAsync();
        var clubs = await _db.SchoolClubs.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Description, x.Schedule, x.Coordinator }).ToListAsync();
        var memberships = await _db.StudentClubMemberships.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .Select(x => x.ClubId).ToListAsync();
        var registrations = await _db.StudentEventRegistrations.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .Select(x => x.EventId).ToListAsync();
        var lostFound = await _db.SchoolLostFoundPosts.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.IsActive && (x.IsApproved || x.StudentId == student.Id))
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.Kind, x.Title, x.Description, x.IsApproved, x.CreatedAt, mine = x.StudentId == student.Id }).Take(100).ToListAsync();
        var house = await (from membership in _db.StudentHouseMemberships.AsNoTracking()
            join h in _db.SchoolHouses on membership.HouseId equals h.Id
            where membership.SchoolId == student.SchoolId && membership.StudentId == student.Id && membership.IsActive && h.IsActive
            select new { h.Name, h.Points }).FirstOrDefaultAsync();
        var transportAlerts = await _db.StudentTransportAlerts.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.IsActive &&
            (x.StudentId == null || x.StudentId == student.Id) && x.EffectiveDate >= DateTime.Today.AddDays(-7) && x.EffectiveDate <= DateTime.Today.AddDays(30))
            .OrderByDescending(x => x.EffectiveDate).Select(x => new { x.Id, x.Title, x.Message, x.EffectiveDate }).ToListAsync();
        var reservations = await (from r in _db.StudentLibraryReservations.AsNoTracking()
            join b in _db.InventoryBooks on r.BookId equals b.Id
            where r.SchoolId == student.SchoolId && r.StudentId == student.Id && r.IsActive
            orderby r.RequestedAt descending
            select new { r.Id, r.BookId, b.BookName, r.Status, r.RequestedAt }).ToListAsync();
        var threads = grade >= 9 ? await _db.StudentDiscussionThreads.AsNoTracking().Where(x => x.SchoolId == student.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.Title, x.CreatedAt }).ToListAsync() : new();
        var token = await _db.StudentIdentityTokens.FirstOrDefaultAsync(x => x.StudentId == student.Id && x.SchoolId == student.SchoolId);
        if (token == null)
        {
            token = new StudentIdentityToken { SchoolId = student.SchoolId, StudentId = student.Id, Token = Guid.NewGuid().ToString("N") };
            _db.StudentIdentityTokens.Add(token);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { token = await _db.StudentIdentityTokens.AsNoTracking().FirstAsync(x => x.StudentId == student.Id && x.SchoolId == student.SchoolId); }
        }
        return Ok(new { success = true, data = new { grade, studentId = student.Id, studentName = student.StudentName,
            dateOfBirth = student.DOB, address = student.Address, classmates, clubs, memberships, registrations,
            lostFound, house, transportAlerts, reservations, threads, identityToken = token.Token } });
    }

    [HttpPost("clubs/{id:int}/join")]
    public async Task<IActionResult> JoinClub(int id)
    {
        var (student, enrollment, _) = await Current(); if (student == null || enrollment == null) return Forbid();
        if (!await _db.SchoolClubs.AnyAsync(x => x.Id == id && x.SchoolId == student.SchoolId && x.IsActive)) return NotFound();
        if (await _db.StudentClubMemberships.AnyAsync(x => x.ClubId == id && x.StudentId == student.Id && x.IsActive)) return Conflict(new { message = "Already joined." });
        var old = await _db.StudentClubMemberships.FirstOrDefaultAsync(x => x.ClubId == id && x.StudentId == student.Id);
        if (old == null) _db.StudentClubMemberships.Add(new StudentClubMembership { SchoolId = student.SchoolId, ClubId = id, StudentId = student.Id });
        else { old.IsActive = true; old.JoinedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("events/{id:int}/register")]
    public async Task<IActionResult> RegisterEvent(int id)
    {
        var (student, enrollment, _) = await Current(); if (student == null || enrollment == null) return Forbid();
        if (!await _db.SchoolCalendarEvents.AnyAsync(x => x.Id == id && x.SchoolId == student.SchoolId && x.IsActive &&
            (x.SectionId == null || x.SectionId == enrollment.SectionId) && x.EventDate >= DateTime.Today)) return NotFound();
        if (await _db.StudentEventRegistrations.AnyAsync(x => x.EventId == id && x.StudentId == student.Id && x.IsActive)) return Conflict(new { message = "Already registered." });
        var old = await _db.StudentEventRegistrations.FirstOrDefaultAsync(x => x.EventId == id && x.StudentId == student.Id);
        if (old == null) _db.StudentEventRegistrations.Add(new StudentEventRegistration { SchoolId = student.SchoolId, EventId = id, StudentId = student.Id });
        else { old.IsActive = true; old.RegisteredAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("lost-found")]
    public async Task<IActionResult> CreateLostFound([FromBody] LostFoundInput input)
    {
        var (student, enrollment, _) = await Current(); if (student == null || enrollment == null) return Forbid();
        if (input.Kind != "Lost" && input.Kind != "Found" || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 ||
            string.IsNullOrWhiteSpace(input.Description) || input.Description.Length > 1000) return BadRequest(new { message = "Enter a valid item and description." });
        _db.SchoolLostFoundPosts.Add(new SchoolLostFoundPost { SchoolId = student.SchoolId, StudentId = student.Id,
            Kind = input.Kind, Title = input.Title.Trim(), Description = input.Description.Trim() });
        await _db.SaveChangesAsync(); return Ok(new { success = true, message = "Submitted for school approval." });
    }
    [HttpGet("discussions/{id:int}/posts")]
    public async Task<IActionResult> DiscussionPosts(int id)
    {
        var (student, enrollment, grade) = await Current(); if (student == null || enrollment == null || grade < 9) return Forbid();
        if (!await _db.StudentDiscussionThreads.AnyAsync(x => x.Id == id && x.SchoolId == student.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive)) return NotFound();
        var posts = await _db.StudentDiscussionPosts.AsNoTracking().Where(x => x.ThreadId == id && x.SchoolId == student.SchoolId && x.IsActive && (x.IsApproved || x.StudentId == student.Id))
            .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.Body, x.CreatedAt, x.IsApproved, mine = x.StudentId == student.Id, byTeacher = x.StaffId != null }).ToListAsync();
        return Ok(new { success = true, data = posts });
    }
    [HttpPost("discussions/{id:int}/posts")]
    public async Task<IActionResult> CreateDiscussionPost(int id, [FromBody] DiscussionPostInput input)
    {
        var (student, enrollment, grade) = await Current(); if (student == null || enrollment == null || grade < 9) return Forbid();
        if (!await _db.StudentDiscussionThreads.AnyAsync(x => x.Id == id && x.SchoolId == student.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive)) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000) return BadRequest(new { message = "Enter a message up to 2000 characters." });
        _db.StudentDiscussionPosts.Add(new StudentDiscussionPost { SchoolId = student.SchoolId, ThreadId = id, StudentId = student.Id, Body = input.Body.Trim() });
        await _db.SaveChangesAsync(); return Ok(new { success = true, message = "Your post is waiting for teacher approval." });
    }
    [HttpPost("library/{id:int}/reserve")]
    public async Task<IActionResult> ReserveBook(int id)
    {
        var (student, enrollment, _) = await Current(); if (student == null || enrollment == null) return Forbid();
        if (!await _db.InventoryBooks.AnyAsync(x => x.Id == id && x.SchoolId == student.SchoolId && x.AcademicSessionId == enrollment.SessionId &&
            (x.ClassId == null || x.ClassId == enrollment.ClassId) && (x.SectionId == null || x.SectionId == enrollment.SectionId))) return NotFound();
        if (await _db.StudentLibraryReservations.AnyAsync(x => x.BookId == id && x.StudentId == student.Id && x.IsActive && (x.Status == "Pending" || x.Status == "Ready"))) return Conflict(new { message = "Already reserved." });
        _db.StudentLibraryReservations.Add(new StudentLibraryReservation { SchoolId = student.SchoolId, StudentId = student.Id, BookId = id });
        await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
}
public class LostFoundInput { public string Kind { get; set; } = "Lost"; public string Title { get; set; } = ""; public string Description { get; set; } = ""; }
public class DiscussionPostInput { public string Body { get; set; } = ""; }
