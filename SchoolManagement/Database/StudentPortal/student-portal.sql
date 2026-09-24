IF OBJECT_ID(N'dbo.ClassDiaryEntries', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ClassDiaryEntries (
   Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
   SchoolId int NOT NULL, SectionId int NOT NULL, SubjectId int NOT NULL, StaffId int NOT NULL,
   EntryDate datetime2 NOT NULL, Topic nvarchar(500) NOT NULL, Pages nvarchar(100) NULL,
   Homework nvarchar(2000) NULL, IsPublished bit NOT NULL DEFAULT 1, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_ClassDiaryEntries_Section_Date ON dbo.ClassDiaryEntries(SchoolId,SectionId,EntryDate);
END;
IF OBJECT_ID(N'dbo.AssignmentSubmissions', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AssignmentSubmissions (
   Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
   SchoolId int NOT NULL, AssignmentId int NOT NULL, StudentId int NOT NULL, EnrollmentId int NOT NULL,
   SubmittedAt datetime2 NOT NULL, TextAnswer nvarchar(4000) NULL, FileUrl nvarchar(1000) NULL,
   Status nvarchar(30) NOT NULL DEFAULT N'Submitted', TeacherFeedback nvarchar(2000) NULL,
   Marks decimal(18,2) NULL, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE UNIQUE INDEX UX_AssignmentSubmissions_Student_Work ON dbo.AssignmentSubmissions(AssignmentId,StudentId) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.SchoolAnnouncements', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SchoolAnnouncements (
   Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
   SchoolId int NOT NULL, SectionId int NULL, CreatedBy int NOT NULL,
   Title nvarchar(200) NOT NULL, Body nvarchar(4000) NOT NULL,
   CreatedAt datetime2 NOT NULL, ExpiresAt datetime2 NULL,
   IsPinned bit NOT NULL DEFAULT 0, IsPublished bit NOT NULL DEFAULT 1, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_SchoolAnnouncements_School_Section ON dbo.SchoolAnnouncements(SchoolId,SectionId,CreatedAt);
END;

