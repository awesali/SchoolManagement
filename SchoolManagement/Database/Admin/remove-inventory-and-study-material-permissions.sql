-- Run after the updated backend/frontend are deployed.
-- Removes retired ADMIN permission catalog entries only. Inventory content tables
-- and TeacherStudyMaterials remain because student/teacher features use them.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @PageIds TABLE (Id int PRIMARY KEY);
INSERT INTO @PageIds (Id)
SELECT Id FROM dbo.ErpPages
WHERE [Key] IN (N'inventory.main', N'study-materials.main',
                N'management.inventory', N'management.study-materials');

DECLARE @PermissionIds TABLE (Id int PRIMARY KEY);
INSERT INTO @PermissionIds (Id)
SELECT Id FROM dbo.Permissions WHERE PageId IN (SELECT Id FROM @PageIds);

DELETE FROM dbo.RolePermissions WHERE PermissionId IN (SELECT Id FROM @PermissionIds);
DELETE FROM dbo.EmployeePermissions WHERE PermissionId IN (SELECT Id FROM @PermissionIds);
DELETE FROM dbo.PermissionOverrides WHERE PermissionId IN (SELECT Id FROM @PermissionIds);
DELETE FROM dbo.Permissions WHERE Id IN (SELECT Id FROM @PermissionIds);
DELETE FROM dbo.ErpPages WHERE Id IN (SELECT Id FROM @PageIds);
DELETE FROM dbo.ErpModules
WHERE [Key] IN (N'inventory', N'study-materials')
  AND NOT EXISTS (SELECT 1 FROM dbo.ErpPages WHERE ModuleId = dbo.ErpModules.Id);

COMMIT TRANSACTION;