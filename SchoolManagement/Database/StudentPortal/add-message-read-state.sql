-- Existing messages remain intact. A NULL ReadAt means the recipient has not opened the message.
IF COL_LENGTH(N'dbo.TeacherStudentMessages', N'ReadAt') IS NULL
    ALTER TABLE dbo.TeacherStudentMessages ADD ReadAt datetime2 NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TeacherStudentMessages') AND name = N'IX_TeacherStudentMessages_Thread')
    CREATE INDEX IX_TeacherStudentMessages_Thread ON dbo.TeacherStudentMessages(SchoolId, StaffId, StudentId, SentAt) INCLUDE (FromStudent, ReadAt) WHERE IsActive = 1;