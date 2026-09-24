IF OBJECT_ID(N'dbo.StudentServiceRequests', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.StudentServiceRequests(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, EnrollmentId int NOT NULL,
 Type nvarchar(40) NOT NULL, Subject nvarchar(200) NOT NULL, Details nvarchar(2000) NOT NULL,
 FromDate datetime2 NULL, ToDate datetime2 NULL, Status nvarchar(20) NOT NULL DEFAULT N'Pending',
 Response nvarchar(2000) NULL, CreatedAt datetime2 NOT NULL, RespondedAt datetime2 NULL, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_StudentServiceRequests_Student ON dbo.StudentServiceRequests(SchoolId,StudentId,CreatedAt);
END;
IF OBJECT_ID(N'dbo.TeacherStudentMessages', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.TeacherStudentMessages(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, StaffId int NOT NULL,
 Body nvarchar(2000) NOT NULL, FromStudent bit NOT NULL, SentAt datetime2 NOT NULL, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_TeacherStudentMessages_Thread ON dbo.TeacherStudentMessages(SchoolId,StudentId,StaffId,SentAt);
END;
IF OBJECT_ID(N'dbo.StudentAchievements', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.StudentAchievements(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL,
 Title nvarchar(200) NOT NULL, Description nvarchar(1000) NULL, AwardedAt datetime2 NOT NULL, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_StudentAchievements_Student ON dbo.StudentAchievements(SchoolId,StudentId,AwardedAt);
END;
IF OBJECT_ID(N'dbo.SchoolCalendarEvents', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SchoolCalendarEvents(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, SectionId int NULL,
 Title nvarchar(200) NOT NULL, Description nvarchar(1000) NULL, EventDate datetime2 NOT NULL, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE INDEX IX_SchoolCalendarEvents_School_Date ON dbo.SchoolCalendarEvents(SchoolId,EventDate);
END;

