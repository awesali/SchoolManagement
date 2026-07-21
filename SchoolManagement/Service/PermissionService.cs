using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using System.Security.Claims;

namespace SchoolManagement.Service;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string key);
    Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId);
}

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    public PermissionService(AppDbContext db) => _db = db;

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string key)
    {
        var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleIdText = principal.FindFirstValue("RoleId");
        if (!int.TryParse(userIdText, out var userId)) return false;
        if (roleIdText == "1") return true; // protected platform owner
        return (await EffectivePermissionsAsync(userId)).Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user?.RoleId == 1) return await _db.Permissions.Where(x => x.IsActive).Select(x => x.Key).ToListAsync();
        var roleIds = await _db.EmployeeRoles.Where(x => x.UserId == userId && x.IsActive).Select(x => x.RoleId).ToListAsync();
        if (user?.RoleId is int primary && !roleIds.Contains(primary)) roleIds.Add(primary);
        var grants = await _db.RolePermissions.Where(x => roleIds.Contains(x.RoleId) && x.IsAllowed).Select(x => x.PermissionId).ToListAsync();
        var direct = await _db.EmployeePermissions.Where(x => x.UserId == userId && x.IsAllowed).Select(x => x.PermissionId).ToListAsync();
        var overrides = await _db.PermissionOverrides.Where(x => x.UserId == userId && x.IsAllowed != null).ToDictionaryAsync(x => x.PermissionId, x => x.IsAllowed!.Value);
        var ids = grants.Concat(direct).Distinct().Where(id => !overrides.TryGetValue(id, out var allowed) || allowed).Concat(overrides.Where(x => x.Value).Select(x => x.Key)).Distinct();
        return await _db.Permissions.Where(x => ids.Contains(x.Id) && x.IsActive).Select(x => x.Key).ToListAsync();
    }
}
