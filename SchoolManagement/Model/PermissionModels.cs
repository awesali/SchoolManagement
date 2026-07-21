using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class ErpModule { public int Id { get; set; } [MaxLength(80)] public string Key { get; set; } = ""; [MaxLength(120)] public string Name { get; set; } = ""; public int SortOrder { get; set; } public bool IsActive { get; set; } = true; }
public class ErpPage { public int Id { get; set; } public int ModuleId { get; set; } [MaxLength(100)] public string Key { get; set; } = ""; [MaxLength(120)] public string Name { get; set; } = ""; public int SortOrder { get; set; } public bool IsActive { get; set; } = true; }
public class ErpAction { public int Id { get; set; } [MaxLength(60)] public string Key { get; set; } = ""; [MaxLength(100)] public string Name { get; set; } = ""; public int SortOrder { get; set; } public bool IsActive { get; set; } = true; }
public class Permission { public int Id { get; set; } public int PageId { get; set; } public int ActionId { get; set; } [MaxLength(220)] public string Key { get; set; } = ""; public bool IsActive { get; set; } = true; }
public class RolePermission { public int Id { get; set; } public int RoleId { get; set; } public int PermissionId { get; set; } public bool IsAllowed { get; set; } public DateTime ModifiedAt { get; set; } public int? ModifiedBy { get; set; } }
public class EmployeeRole { public int Id { get; set; } public int UserId { get; set; } public int RoleId { get; set; } public bool IsActive { get; set; } = true; }
public class EmployeePermission { public int Id { get; set; } public int UserId { get; set; } public int PermissionId { get; set; } public bool IsAllowed { get; set; } public DateTime ModifiedAt { get; set; } public int? ModifiedBy { get; set; } }
public class PermissionOverride { public int Id { get; set; } public int UserId { get; set; } public int PermissionId { get; set; } public bool? IsAllowed { get; set; } public DateTime ModifiedAt { get; set; } public int? ModifiedBy { get; set; } }
public class FeatureFlag { public int Id { get; set; } public int? SchoolId { get; set; } [MaxLength(100)] public string Key { get; set; } = ""; [MaxLength(150)] public string Name { get; set; } = ""; public bool IsEnabled { get; set; } public DateTime ModifiedAt { get; set; } public int? ModifiedBy { get; set; } }
public class PermissionAuditLog { public long Id { get; set; } public int? UserId { get; set; } [MaxLength(80)] public string EntityType { get; set; } = ""; [MaxLength(80)] public string EntityId { get; set; } = ""; [MaxLength(100)] public string Action { get; set; } = ""; public string? OldValue { get; set; } public string? NewValue { get; set; } [MaxLength(64)] public string? IpAddress { get; set; } public DateTime CreatedAt { get; set; } }
public class UserSchoolAccess { public int Id { get; set; } public int UserId { get; set; } public int? SchoolId { get; set; } public bool AllSchools { get; set; } }
public class UserSessionAccess { public int Id { get; set; } public int UserId { get; set; } public int AcademicSessionId { get; set; } }
