-- Add explicit recipient routing for student requests. Nullable columns preserve existing requests.
IF COL_LENGTH(N'dbo.StudentServiceRequests', N'RecipientRoleId') IS NULL
    ALTER TABLE dbo.StudentServiceRequests ADD RecipientRoleId int NULL;
IF COL_LENGTH(N'dbo.StudentServiceRequests', N'RecipientUserId') IS NULL
    ALTER TABLE dbo.StudentServiceRequests ADD RecipientUserId int NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StudentServiceRequests') AND name = N'IX_StudentServiceRequests_Recipient')
    CREATE INDEX IX_StudentServiceRequests_Recipient ON dbo.StudentServiceRequests(RecipientUserId, RecipientRoleId, CreatedAt) WHERE RecipientUserId IS NOT NULL;