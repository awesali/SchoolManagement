using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/SchoolCommunityAdmin")]
public class SchoolCommunityAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public SchoolCommunityAdminController(AppDbContext db) => _db = db;
    private bool CanManage(int schoolId)
    {
        var roleId = User.FindFirstValue("RoleId");
        return roleId == "1" || (roleId != null && roleId != "2" && !User.IsInRole("Student") &&
            int.TryParse(User.FindFirstValue("SchoolId"), out var ownSchool) && ownSchool == schoolId);
    }
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] int schoolId)
    {
        if (!CanManage(schoolId)) return Forbid();
        var clubs = await _db.SchoolClubs.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).ToListAsync();
        var houses = await _db.SchoolHouses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).ToListAsync();
        var houseMemberships = await (from m in _db.StudentHouseMemberships.AsNoTracking()
            join s in _db.Students on m.StudentId equals s.Id
            where m.SchoolId == schoolId && m.IsActive
            select new { m.StudentId, s.StudentName, m.HouseId }).ToListAsync();
        var transportAlerts = await _db.StudentTransportAlerts.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive).OrderByDescending(x => x.EffectiveDate).Take(100).ToListAsync();
        var lostFound = await (from p in _db.SchoolLostFoundPosts.AsNoTracking()
            join s in _db.Students on p.StudentId equals s.Id
            where p.SchoolId == schoolId && p.IsActive
            orderby p.CreatedAt descending
            select new { p.Id, p.Kind, p.Title, p.Description, p.IsApproved, p.CreatedAt, s.StudentName }).Take(100).ToListAsync();
        var reservations = await (from r in _db.StudentLibraryReservations.AsNoTracking()
            join s in _db.Students on r.StudentId equals s.Id
            join b in _db.InventoryBooks on r.BookId equals b.Id
            where r.SchoolId == schoolId && r.IsActive
            orderby r.RequestedAt descending
            select new { r.Id, s.StudentName, b.BookName, r.Status, r.RequestedAt }).Take(100).ToListAsync();
        var eventRegistrations = await (from r in _db.StudentEventRegistrations.AsNoTracking()
            join s in _db.Students on r.StudentId equals s.Id
            join e in _db.SchoolCalendarEvents on r.EventId equals e.Id
            where r.SchoolId == schoolId && r.IsActive
            select new { r.Id, s.StudentName, eventName = e.Title, r.RegisteredAt }).ToListAsync();
        var clubMemberships = await (from m in _db.StudentClubMemberships.AsNoTracking()
            join s in _db.Students on m.StudentId equals s.Id
            join c in _db.SchoolClubs on m.ClubId equals c.Id
            where m.SchoolId == schoolId && m.IsActive
            select new { m.Id, s.StudentName, clubName = c.Name, m.JoinedAt }).ToListAsync();
        return Ok(new { success = true, data = new { clubs, houses, houseMemberships, transportAlerts, lostFound, reservations, eventRegistrations, clubMemberships } });
    }
    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClub([FromBody] ClubInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 120 || input.Description?.Length > 1000 || input.Schedule?.Length > 200 || input.Coordinator?.Length > 120) return BadRequest();
        var item = new SchoolClub { SchoolId = input.SchoolId, Name = input.Name.Trim(), Description = input.Description?.Trim(), Schedule = input.Schedule?.Trim(), Coordinator = input.Coordinator?.Trim() };
        _db.SchoolClubs.Add(item); await _db.SaveChangesAsync(); return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpPost("houses")]
    public async Task<IActionResult> CreateHouse([FromBody] HouseInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100) return BadRequest();
        var item = new SchoolHouse { SchoolId = input.SchoolId, Name = input.Name.Trim() };
        _db.SchoolHouses.Add(item); await _db.SaveChangesAsync(); return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpPost("houses/assign")]
    public async Task<IActionResult> AssignHouse([FromBody] HouseAssignmentInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (!await _db.Students.AnyAsync(x => x.Id == input.StudentId && x.SchoolId == input.SchoolId && x.IsActive) ||
            !await _db.SchoolHouses.AnyAsync(x => x.Id == input.HouseId && x.SchoolId == input.SchoolId && x.IsActive)) return BadRequest();
        var old = await _db.StudentHouseMemberships.FirstOrDefaultAsync(x => x.SchoolId == input.SchoolId && x.StudentId == input.StudentId && x.IsActive);
        if (old == null) _db.StudentHouseMemberships.Add(new StudentHouseMembership { SchoolId = input.SchoolId, StudentId = input.StudentId, HouseId = input.HouseId });
        else old.HouseId = input.HouseId;
        await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("houses/{id:int}/points")]
    public async Task<IActionResult> SetHousePoints(int id, [FromBody] HousePointsInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        var house = await _db.SchoolHouses.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == input.SchoolId && x.IsActive);
        if (house == null) return NotFound(); if (input.Points < 0) return BadRequest();
        house.Points = input.Points; await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("transport-alerts")]
    public async Task<IActionResult> TransportAlert([FromBody] TransportAlertInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || string.IsNullOrWhiteSpace(input.Message) || input.Message.Length > 1000 || input.EffectiveDate == default) return BadRequest();
        if (input.StudentId.HasValue && !await _db.Students.AnyAsync(x => x.Id == input.StudentId && x.SchoolId == input.SchoolId && x.IsActive)) return BadRequest();
        var item = new StudentTransportAlert { SchoolId = input.SchoolId, StudentId = input.StudentId,
            Title = input.Title.Trim(), Message = input.Message.Trim(), EffectiveDate = input.EffectiveDate.Date };
        _db.StudentTransportAlerts.Add(item); await _db.SaveChangesAsync(); return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpPost("lost-found/{id:int}/review")]
    public async Task<IActionResult> ReviewLostFound(int id, [FromBody] ReviewInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        var item = await _db.SchoolLostFoundPosts.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == input.SchoolId && x.IsActive);
        if (item == null) return NotFound(); item.IsApproved = input.Approve; await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
    [HttpPost("library-reservations/{id:int}/status")]
    public async Task<IActionResult> SetReservationStatus(int id, [FromBody] ReservationStatusInput input)
    {
        if (!CanManage(input.SchoolId)) return Forbid();
        if (!new[] { "Pending", "Ready", "Collected", "Cancelled" }.Contains(input.Status)) return BadRequest();
        var item = await _db.StudentLibraryReservations.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == input.SchoolId && x.IsActive);
        if (item == null) return NotFound(); item.Status = input.Status; await _db.SaveChangesAsync(); return Ok(new { success = true });
    }
}
public class ClubInput { public int SchoolId { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } public string? Schedule { get; set; } public string? Coordinator { get; set; } }
public class HouseInput { public int SchoolId { get; set; } public string Name { get; set; } = ""; }
public class HouseAssignmentInput { public int SchoolId { get; set; } public int StudentId { get; set; } public int HouseId { get; set; } }
public class HousePointsInput { public int SchoolId { get; set; } public int Points { get; set; } }
public class TransportAlertInput { public int SchoolId { get; set; } public int? StudentId { get; set; } public string Title { get; set; } = ""; public string Message { get; set; } = ""; public DateTime EffectiveDate { get; set; } }
public class ReviewInput { public int SchoolId { get; set; } public bool Approve { get; set; } }
public class ReservationStatusInput { public int SchoolId { get; set; } public string Status { get; set; } = ""; }
