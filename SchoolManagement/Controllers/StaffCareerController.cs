using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Service;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/staff-career")]
public class StaffCareerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;
    public StaffCareerController(AppDbContext db, IPermissionService permissions) { _db = db; _permissions = permissions; }
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private async Task<bool> CanAccess(int schoolId, string action)
    {
        if (!await _permissions.HasPermissionAsync(User, "management.staff." + action)) return false;
        var caller = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == UserId && x.IsActive);
        if (caller == null) return false;
        return caller.RoleId == 1 || caller.School_Id == schoolId ||
            await _db.Schools.AnyAsync(x => x.Id == schoolId && x.SuperAdminId == UserId) ||
            await _db.UserSchoolAccess.AnyAsync(x => x.UserId == UserId && (x.AllSchools || x.SchoolId == schoolId));
    }

    [HttpPut("{staffId:int}/role")]
    public async Task<IActionResult> ChangeRole(int staffId, RoleChangeRequest request)
    {
        if (!await CanAccess(request.SchoolId, "update")) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return BadRequest(new { message = "Enter a reason (up to 1000 characters)." });
        using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var staff = await _db.Staff.FirstOrDefaultAsync(x => x.Id == staffId && x.SchoolId == request.SchoolId);
        if (staff == null) return NotFound();
        if (!staff.IsActive) return BadRequest(new { message = "Only active staff can be promoted or demoted." });
        if (staff.RoleId != request.ExpectedRoleId) return Conflict(new { message = "The role has changed. Refresh the profile and try again." });
        var current = await _db.Roles.FirstOrDefaultAsync(x => x.Id == staff.RoleId);
        var expectedName = request.Action == "promote" ? "Teacher" : request.Action == "demote" ? "Principal" : null;
        if (expectedName == null || !string.Equals(current?.RoleName, expectedName, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only Teacher to Principal promotions and Principal to Teacher demotions are supported." });
        var targetName = request.Action == "promote" ? "Principal" : "Teacher";
        var target = await _db.Roles.Where(x => x.IsActive && (x.School_Id == request.SchoolId || x.School_Id == null) && x.RoleName == targetName)
            .OrderByDescending(x => x.School_Id == request.SchoolId).FirstOrDefaultAsync();
        if (target == null) return BadRequest(new { message = "The target role is not configured for this school." });
        var login = await _db.Users.FirstOrDefaultAsync(x => x.Id == staff.usersid);
        if (login == null || login.RoleId == 1 || login.School_Id != staff.SchoolId || login.RoleId != staff.RoleId)
            return BadRequest(new { message = "The staff login role does not match this profile. Correct employee access before changing the role." });
        var previousRole = staff.RoleId;
        staff.RoleId = target.Id;
        staff.Modified_Date = DateTime.UtcNow;
        staff.Updated_By = UserId;
        login.RoleId = target.Id;
        // Remove duplicate additional grants for the previous/target primary role.
        var extraRoles = await _db.EmployeeRoles.Where(x => x.UserId == login.Id && (x.RoleId == previousRole || x.RoleId == target.Id)).ToListAsync();
        _db.EmployeeRoles.RemoveRange(extraRoles);
        _db.StaffChangeReason = request.Reason.Trim();
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, message = request.Action == "promote" ? "Promoted to Principal." : "Demoted to Teacher." });
    }

    [HttpGet("{staffId:int}/history")]
    public async Task<IActionResult> History(int staffId, int schoolId, int page = 1)
    {
        if (!await CanAccess(schoolId, "read")) return Forbid();
        if (!await _db.Staff.AnyAsync(x => x.Id == staffId && x.SchoolId == schoolId)) return NotFound();
        page = Math.Max(1, page);
        var query = _db.PermissionAuditLogs.AsNoTracking().Where(x => x.EntityType == "StaffChange" && x.EntityId == staffId.ToString());
        var total = await query.CountAsync();
        var rows = await (from log in query join user in _db.Users on log.UserId equals (int?)user.Id into actors
                          from actor in actors.DefaultIfEmpty() orderby log.CreatedAt descending, log.Id descending
                          select new { log.Id, log.Action, log.CreatedAt, log.UserId, Actor = actor == null ? null : actor.Name, log.OldValue, log.NewValue })
                          .Skip((page - 1) * 50).Take(50).ToListAsync();
        return Ok(new { success = true, totalRecords = total, totalPages = (int)Math.Ceiling(total / 50.0), data = rows.Select(x => new {
            x.Id, x.Action, createdAt = DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc), actor = x.Actor ?? (x.UserId.HasValue ? $"User #{x.UserId}" : "System"),
            before = JsonSerializer.Deserialize<JsonElement>(x.OldValue ?? "{}"), after = JsonSerializer.Deserialize<JsonElement>(x.NewValue ?? "{}")
        }) });
    }
}
public record RoleChangeRequest(int SchoolId, int ExpectedRoleId, string Action, string Reason);