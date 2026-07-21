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

    [HttpGet("me")]
    public async Task<IActionResult> Me() => Ok(new { permissions = await _service.EffectivePermissionsAsync(UserId), features = await _db.FeatureFlags.Where(x => x.SchoolId == null || x.SchoolId.ToString() == User.FindFirstValue("SchoolId")).ToDictionaryAsync(x => x.Key, x => x.IsEnabled) });

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
        Roles role;
        if (request.Id is int id) { role = await _db.Roles.FindAsync(id) ?? throw new KeyNotFoundException(); role.RoleName = request.Name.Trim(); role.Description = request.Description; role.IsActive = request.IsActive; role.Modified_Date = DateTime.UtcNow; }
        else { role = new Roles { RoleName = request.Name.Trim(), Description = request.Description, IsActive = request.IsActive, School_Id = request.SchoolId, Created_Date = DateTime.UtcNow, Created_By = UserId }; _db.Roles.Add(role); }
        await _db.SaveChangesAsync(); await Audit("Role", role.Id.ToString(), "Save", null, role); return Ok(role);
    }

    [HttpGet("roles/{roleId:int}")]
    public async Task<IActionResult> RolePermissions(int roleId) => IsSuperAdmin ? Ok(await _db.RolePermissions.Where(x => x.RoleId == roleId && x.IsAllowed).Select(x => x.PermissionId).ToListAsync()) : Forbid();

    [HttpPut("roles/{roleId:int}")]
    public async Task<IActionResult> SavePermissions(int roleId, PermissionIdsRequest request)
    {
        if (!IsSuperAdmin) return Forbid();
        var old = await _db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(old);
        _db.RolePermissions.AddRange(request.PermissionIds.Distinct().Select(id => new RolePermission { RoleId = roleId, PermissionId = id, IsAllowed = true, ModifiedAt = DateTime.UtcNow, ModifiedBy = UserId }));
        await _db.SaveChangesAsync(); await Audit("RolePermission", roleId.ToString(), "BulkUpdate", old.Select(x => x.PermissionId), request.PermissionIds); return NoContent();
    }

    [HttpPost("roles/{sourceId:int}/copy/{targetId:int}")]
    public async Task<IActionResult> Copy(int sourceId, int targetId) { if (!IsSuperAdmin) return Forbid(); var ids = await _db.RolePermissions.Where(x => x.RoleId == sourceId && x.IsAllowed).Select(x => x.PermissionId).ToListAsync(); return await SavePermissions(targetId, new(ids)); }

    [HttpGet("features")]
    public async Task<IActionResult> Features() => IsSuperAdmin ? Ok(await _db.FeatureFlags.OrderBy(x => x.Name).ToListAsync()) : Forbid();

    [HttpPut("features/{id:int}")]
    public async Task<IActionResult> Feature(int id, FeatureRequest request) { if (!IsSuperAdmin) return Forbid(); var flag = await _db.FeatureFlags.FindAsync(id); if (flag == null) return NotFound(); var old = flag.IsEnabled; flag.IsEnabled = request.Enabled; flag.ModifiedAt = DateTime.UtcNow; flag.ModifiedBy = UserId; await _db.SaveChangesAsync(); await Audit("FeatureFlag", id.ToString(), "Toggle", old, request.Enabled); return NoContent(); }

    private async Task Audit(string type, string id, string action, object? oldValue, object? newValue) { _db.PermissionAuditLogs.Add(new PermissionAuditLog { UserId = UserId, EntityType = type, EntityId = id, Action = action, OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue), NewValue = newValue == null ? null : JsonSerializer.Serialize(newValue), IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow }); await _db.SaveChangesAsync(); }
}
public record RoleRequest(int? Id, string Name, string? Description, bool IsActive, int? SchoolId);
public record PermissionIdsRequest(List<int> PermissionIds);
public record FeatureRequest(bool Enabled);
