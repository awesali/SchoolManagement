// Backend section: application services and shared rules.
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Service;

// Implements permission application behavior.
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string key);
    Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId);
}

public class PermissionService : IPermissionService
{
    // Dependencies and state used by this component.
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    // Scoped service: reuse checks within this request only; never retain grants across requests.
    private readonly Dictionary<int, Task<IReadOnlyList<string>>> _checks = new();

    // Creates the component with its required dependencies.
    public PermissionService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string key)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return false;
        if (!_configuration.GetValue<bool>("Security:CrudPermissionsEnabled", true))
            return true;
        var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleIdText = principal.FindFirstValue("RoleId");
        if (!int.TryParse(userIdText, out var userId))
            return false;
        if (roleIdText == "1")
            return true; // protected platform owner
        if (!_checks.TryGetValue(userId, out var checks))
            _checks[userId] = checks = EffectivePermissionsAsync(userId);
        return (await checks).Contains(
            key,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId)
    {
        // Keep all grant sources in SQL instead of six sequential database round trips.
        var roleIds = _db.EmployeeRoles
            .Where(x => x.UserId == userId && x.IsActive).Select(x => x.RoleId)
            .Union(_db.Users.Where(x => x.Id == userId && x.RoleId.HasValue)
                .Select(x => x.RoleId!.Value));
        var grants = _db.RolePermissions
            .Where(x => roleIds.Contains(x.RoleId) && x.IsAllowed).Select(x => x.PermissionId)
            .Union(_db.EmployeePermissions.Where(x => x.UserId == userId && x.IsAllowed)
                .Select(x => x.PermissionId))
            .Union(_db.PermissionOverrides.Where(x => x.UserId == userId && x.IsAllowed == true)
                .Select(x => x.PermissionId));
        var owner = _db.Users.Where(x => x.Id == userId && x.RoleId == 1);
        return await _db.Permissions.AsNoTracking()
            .Where(x => x.IsActive && (owner.Any() || grants.Contains(x.Id)))
            .Select(x => x.Key).ToListAsync();
    }
}
