using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SchoolManagement.Model;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolManagement.Data;

public partial class AppDbContext
{
    public string? StaffChangeReason { get; set; }

    // Audit rows participate in the same SaveChanges transaction as the changes.
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var logs = new List<PermissionAuditLog>();
        var ignored = new HashSet<string> { "Id", "Created_Date", "Modified_Date", "Created_By", "Updated_By", "CreatedDate", "usersid" };
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            var type = entry.Entity.GetType().Name;
            string? staffProperty = entry.Entity switch
            {
                SchoolManagement.Model.Staff => "Id",
                SchoolManagement.Model.SubjectTeachers or SchoolManagement.Model.SectionSubjectTeachers or SchoolManagement.Model.SectionDetails or StaffDocument => "StaffId",
                ProfilePicture p when p.PersonType == "Staff" => "PersonId",
                _ => null
            };
            if ((staffProperty == null && entry.Entity is not SchoolManagement.Model.Subjects) || (entry.Entity is Staff && entry.State == EntityState.Added)) continue;
            var ids = new HashSet<int>();
            if (staffProperty != null)
            {
                if (entry.State != EntityState.Added && entry.OriginalValues[staffProperty] is int oldId && oldId > 0) ids.Add(oldId);
                if (entry.State != EntityState.Deleted && entry.CurrentValues[staffProperty] is int newId && newId > 0) ids.Add(newId);
            }
            else if (entry.Entity is SchoolManagement.Model.Subjects subject && entry.State != EntityState.Added)
            {
                ids.UnionWith(await SubjectTeachers.Where(x => x.SubjectId == subject.Id && x.IsActive).Select(x => x.StaffId).ToListAsync(cancellationToken));
                ids.UnionWith(await SectionSubjectTeachers.Where(x => x.SubjectId == subject.Id && x.IsActive).Select(x => x.StaffId).ToListAsync(cancellationToken));
                // Include both sides of a reassignment made in this same save.
                foreach (var assignment in ChangeTracker.Entries<SchoolManagement.Model.SubjectTeachers>().Where(x => x.Entity.SubjectId == subject.Id))
                {
                    if (assignment.Entity.StaffId > 0) ids.Add(assignment.Entity.StaffId);
                    if (assignment.State != EntityState.Added && assignment.OriginalValues["StaffId"] is int previous && previous > 0) ids.Add(previous);
                }
            }
            if (ids.Count == 0) continue;
            var before = new Dictionary<string, string?>();
            var after = new Dictionary<string, string?>();
            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (ignored.Contains(name)) continue;
                if (entry.State == EntityState.Modified && (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))) continue;
                before[name] = entry.State == EntityState.Added ? null : await DisplayValue(name, property.OriginalValue, cancellationToken);
                after[name] = entry.State == EntityState.Deleted ? null : await DisplayValue(name, property.CurrentValue, cancellationToken);
            }
            if (after.Count == 0) continue;
            // Retain assignment context even when only its active flag changes.
            foreach (var name in new[] { "SubjectId", "SectionId", "ClassId", "SectionName", "DocumentName" })
            {
                var property = entry.Metadata.FindProperty(name);
                if (property == null || after.ContainsKey(name)) continue;
                before[name] = entry.State == EntityState.Added ? null : await DisplayValue(name, entry.OriginalValues[name], cancellationToken);
                after[name] = entry.State == EntityState.Deleted ? null : await DisplayValue(name, entry.CurrentValues[name], cancellationToken);
            }
            var http = _staffHistoryHttp?.HttpContext;
            foreach (var id in ids)
                logs.Add(new PermissionAuditLog
                {
                    EntityType = "StaffChange", EntityId = id.ToString(),
                    Action = type + ": " + entry.State,
                    UserId = int.TryParse(http?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
                    OldValue = JsonSerializer.Serialize(before),
                    NewValue = JsonSerializer.Serialize(new { values = after, reason = StaffChangeReason, actor = http?.User.Identity?.Name }),
                    CreatedAt = DateTime.UtcNow, IpAddress = http?.Connection.RemoteIpAddress?.ToString()
                });
        }
        PermissionAuditLogs.AddRange(logs);
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch { foreach (var log in logs) Entry(log).State = EntityState.Detached; throw; }
    }

    private async Task<string?> DisplayValue(string name, object? value, CancellationToken ct)
    {
        if (value == null) return null;
        if (value is int id)
        {
            if (name == "RoleId") return await Roles.Where(x => x.Id == id).Select(x => x.RoleName).FirstOrDefaultAsync(ct) ?? $"Role #{id}";
            if (name == "SubjectId") return Subjects.Local.FirstOrDefault(x => x.Id == id)?.SubjectName ?? await Subjects.Where(x => x.Id == id).Select(x => x.SubjectName).FirstOrDefaultAsync(ct) ?? $"Subject #{id}";
            if (name == "SectionId") return await SectionDetails.Where(x => x.Id == id).Select(x => x.SectionName).FirstOrDefaultAsync(ct) ?? $"Section #{id}";
            if (name == "ClassId") return await Classes.Where(x => x.Id == id).Select(x => x.ClassName).FirstOrDefaultAsync(ct) ?? $"Class #{id}";
            if (name == "StaffId") return await Staff.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? $"Staff #{id}";
        }
        return value is DateTime date ? date.ToString("O") : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}