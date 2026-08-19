using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _service;
    public PermissionsController(AppDbContext db, IPermissionService service) { _db = db; _service = service; }
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private bool IsSuperAdmin => User.FindFirstValue("RoleId") == "1";
    private int? SchoolId => int.TryParse(User.FindFirstValue("SchoolId"), out var id) ? id : null;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var roleName = await (from user in _db.Users
                              join role in _db.Roles on user.RoleId equals role.Id
                              where user.Id == UserId
                              select role.RoleName).FirstOrDefaultAsync();
        return Ok(new
        {
            roleName,
            permissions = await _service.EffectivePermissionsAsync(UserId),
            features = await _db.FeatureFlags.Where(x => x.SchoolId == null || x.SchoolId.ToString() == User.FindFirstValue("SchoolId")).ToDictionaryAsync(x => x.Key, x => x.IsEnabled)
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => IsSuperAdmin ? Ok(new {
        totalRoles = await _db.Roles.CountAsync(), totalEmployees = await _db.Users.CountAsync(x => x.IsActive),
        activePermissions = await _db.RolePermissions.CountAsync(x => x.IsAllowed),
        recentlyModified = await _db.PermissionAuditLogs.OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync()
    }) : Forbid();

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog()
    {
        if (!IsSuperAdmin) return Forbid();
        var actions = await _db.ErpActions.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        var permissions = await _db.Permissions.ToListAsync();
        var pages = await _db.ErpPages.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        var modules = await _db.ErpModules.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        return Ok(modules.Select(m => new { m.Id, m.Key, m.Name, pages = pages.Where(p => p.ModuleId == m.Id).Select(p => new { p.Id, p.Key, p.Name, permissions = actions.Select(a => new { actionId = a.Id, actionKey = a.Key, actionName = a.Name, permissionId = permissions.FirstOrDefault(x => x.PageId == p.Id && x.ActionId == a.Id)?.Id }) }) }));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles() => IsSuperAdmin ? Ok(await _db.Roles.OrderBy(x => x.RoleName).Select(x => new { x.Id, x.RoleName, x.Description, x.IsActive }).ToListAsync()) : Forbid();

    [HttpPost("roles")]
    public async Task<IActionResult> SaveRole(RoleRequest request)
    {
        if (!IsSuperAdmin) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Role name is required." });
        if (request.Id == 1) return BadRequest(new { message = "The protected Super Admin role cannot be changed." });
        var schoolId = request.SchoolId ?? SchoolId;
        if (await _db.Roles.AnyAsync(x => x.Id != request.Id && x.School_Id == schoolId && x.RoleName.ToLower() == request.Name.Trim().ToLower()))
            return Conflict(new { message = "A role with this name already exists for this school." });
        Roles role;
        if (request.Id is int id) { role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id && (x.School_Id == schoolId || x.School_Id == null)) ?? throw new KeyNotFoundException(); role.RoleName = request.Name.Trim(); role.Description = request.Description; role.IsActive = request.IsActive; role.Modified_Date = DateTime.UtcNow; role.Updated_By = UserId; }
        else { role = new Roles { RoleName = request.Name.Trim(), Description = request.Description, IsActive = request.IsActive, School_Id = schoolId, Created_Date = DateTime.UtcNow, Created_By = UserId }; _db.Roles.Add(role); }
        await _db.SaveChangesAsync(); await Audit("Role", role.Id.ToString(), "Save", null, role); return Ok(role);
    }

    [HttpGet("roles/{roleId:int}")]
    public async Task<IActionResult> RolePermissions(int roleId) => IsSuperAdmin ? Ok(await _db.RolePermissions.Where(x => x.RoleId == roleId && x.IsAllowed).Select(x => x.PermissionId).ToListAsync()) : Forbid();

    [HttpPut("roles/{roleId:int}")]
    public async Task<IActionResult> SavePermissions(int roleId, PermissionIdsRequest request)
    {
        if (!IsSuperAdmin) return Forbid();
        if (roleId == 1) return BadRequest(new { message = "Super Admin always has every permission and cannot be restricted." });
        if (!await _db.Roles.AnyAsync(x => x.Id == roleId && (x.School_Id == SchoolId || x.School_Id == null))) return NotFound();
        var valid = await _db.Permissions.Where(x => x.IsActive && request.PermissionIds.Contains(x.Id)).Select(x => new { x.Id, x.PageId, x.ActionId }).ToListAsync();
        var readActionId = await _db.ErpActions.Where(x => x.Key == "read").Select(x => x.Id).FirstOrDefaultAsync();
        var selectedPages = valid.Where(x => x.ActionId != readActionId).Select(x => x.PageId).Distinct().ToList();
        var requiredReads = await _db.Permissions.Where(x => selectedPages.Contains(x.PageId) && x.ActionId == readActionId && x.IsActive).Select(x => x.Id).ToListAsync();
        var permissionIds = valid.Select(x => x.Id).Concat(requiredReads).Distinct().ToList();
        var old = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(old);
        _db.RolePermissions.AddRange(permissionIds.Select(id => new RolePermission { RoleId = roleId, PermissionId = id, IsAllowed = true, ModifiedAt = DateTime.UtcNow, ModifiedBy = UserId }));
        await _db.SaveChangesAsync(); await Audit("RolePermission", roleId.ToString(), "BulkUpdate", old.Select(x => x.PermissionId), permissionIds); return NoContent();
    }

    [HttpPost("roles/{sourceId:int}/copy/{targetId:int}")]
    public async Task<IActionResult> Copy(int sourceId, int targetId) { if (!IsSuperAdmin) return Forbid(); var ids = await _db.RolePermissions.Where(x => x.RoleId == sourceId && x.IsAllowed).Select(x => x.PermissionId).ToListAsync(); return await SavePermissions(targetId, new(ids)); }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(int? roleId = null, string? search = null, int? schoolId = null)
    {
        if (!IsSuperAdmin) return Forbid();
        var query = from u in _db.Users.AsNoTracking()
                    join r in _db.Roles.AsNoTracking() on u.RoleId equals r.Id into roles
                    from r in roles.DefaultIfEmpty()
                    where u.Id != UserId && u.RoleId != 1 && (!schoolId.HasValue || u.School_Id == schoolId.Value)
                    select new { u.Id, u.Name, u.Email, u.RoleId, RoleName = r == null ? null : r.RoleName, u.IsActive };
        if (roleId.HasValue) query = query.Where(x => x.RoleId == roleId.Value);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLower(); query = query.Where(x => x.Name.ToLower().Contains(term) || x.Email.ToLower().Contains(term)); }
        return Ok(await query.OrderBy(x => x.Name).ToListAsync());
    }

    [HttpGet("employees/{userId:int}")]
    public async Task<IActionResult> EmployeeAccess(int userId, int? schoolId = null)
    {
        if (!IsSuperAdmin) return Forbid();
        var employee = await _db.Users.AsNoTracking().Where(x => x.Id == userId && x.RoleId != 1 && (!schoolId.HasValue || x.School_Id == schoolId.Value))
            .Select(x => new { x.Id, x.Name, x.Email, x.RoleId, x.IsActive }).FirstOrDefaultAsync();
        if (employee == null) return NotFound();
        var roleIds = await _db.EmployeeRoles.Where(x => x.UserId == userId && x.IsActive).Select(x => x.RoleId).ToListAsync();
        var overrides = await _db.PermissionOverrides.Where(x => x.UserId == userId && x.IsAllowed == true).Select(x => new { x.PermissionId, x.IsAllowed }).ToListAsync();
        return Ok(new { employee, additionalRoleIds = roleIds, overrides, effectivePermissions = await _service.EffectivePermissionsAsync(userId) });
    }

    [HttpPut("employees/{userId:int}")]
    public async Task<IActionResult> SaveEmployeeAccess(int userId, EmployeeAccessRequest request, int? schoolId = null)
    {
        if (!IsSuperAdmin) return Forbid();
        var employee = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Id != UserId && x.RoleId != 1 && (!schoolId.HasValue || x.School_Id == schoolId.Value));
        if (employee == null) return BadRequest(new { message = "Employee was not found or is protected." });
        if (!await _db.Roles.AnyAsync(x => x.Id == request.PrimaryRoleId && x.IsActive && (x.School_Id == employee.School_Id || x.School_Id == null))) return BadRequest(new { message = "Primary role is invalid." });
        var allowedRoleIds = await _db.Roles.Where(x => request.AdditionalRoleIds.Contains(x.Id) && x.Id != 1 && x.IsActive && (x.School_Id == employee.School_Id || x.School_Id == null)).Select(x => x.Id).ToListAsync();
        var validPermissionIds = await _db.Permissions.Where(x => request.Overrides.Select(o => o.PermissionId).Contains(x.Id) && x.IsActive).Select(x => x.Id).ToListAsync();
        var old = new { employee.RoleId, Roles = await _db.EmployeeRoles.Where(x => x.UserId == userId && x.IsActive).Select(x => x.RoleId).ToListAsync(), Overrides = await _db.PermissionOverrides.Where(x => x.UserId == userId).Select(x => new { x.PermissionId, x.IsAllowed }).ToListAsync() };
        employee.RoleId = request.PrimaryRoleId;
        _db.EmployeeRoles.RemoveRange(await _db.EmployeeRoles.Where(x => x.UserId == userId).ToListAsync());
        _db.EmployeeRoles.AddRange(allowedRoleIds.Distinct().Where(x => x != request.PrimaryRoleId).Select(x => new EmployeeRole { UserId = userId, RoleId = x, IsActive = true }));
        _db.PermissionOverrides.RemoveRange(await _db.PermissionOverrides.Where(x => x.UserId == userId).ToListAsync());
        _db.PermissionOverrides.AddRange(request.Overrides.Where(x => x.IsAllowed == true && validPermissionIds.Contains(x.PermissionId)).GroupBy(x => x.PermissionId).Select(x => x.Last()).Select(x => new PermissionOverride { UserId = userId, PermissionId = x.PermissionId, IsAllowed = true, ModifiedAt = DateTime.UtcNow, ModifiedBy = UserId }));
        await _db.SaveChangesAsync();
        await Audit("EmployeeAccess", userId.ToString(), "Update", old, request);
        return Ok(new { effectivePermissions = await _service.EffectivePermissionsAsync(userId) });
    }

    [HttpGet("features")]
    public async Task<IActionResult> Features() => IsSuperAdmin ? Ok(await _db.FeatureFlags.OrderBy(x => x.Name).ToListAsync()) : Forbid();

    [HttpPut("features/{id:int}")]
    public async Task<IActionResult> Feature(int id, FeatureRequest request) { if (!IsSuperAdmin) return Forbid(); var flag = await _db.FeatureFlags.FindAsync(id); if (flag == null) return NotFound(); var old = flag.IsEnabled; flag.IsEnabled = request.Enabled; flag.ModifiedAt = DateTime.UtcNow; flag.ModifiedBy = UserId; await _db.SaveChangesAsync(); await Audit("FeatureFlag", id.ToString(), "Toggle", old, request.Enabled); return NoContent(); }

    private async Task Audit(string type, string id, string action, object? oldValue, object? newValue) { _db.PermissionAuditLogs.Add(new PermissionAuditLog { UserId = UserId, EntityType = type, EntityId = id, Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = newValue == null ? null : JsonSerializer.Serialize(newValue), IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow }); await _db.SaveChangesAsync(); }
}
public record RoleRequest(int? Id, string Name, string? Description, bool IsActive, int? SchoolId);
public record PermissionIdsRequest(List<int> PermissionIds);
public record FeatureRequest(bool Enabled);
public record PermissionOverrideRequest(int PermissionId, bool? IsAllowed);
public record EmployeeAccessRequest(int PrimaryRoleId, List<int> AdditionalRoleIds, List<PermissionOverrideRequest> Overrides);
