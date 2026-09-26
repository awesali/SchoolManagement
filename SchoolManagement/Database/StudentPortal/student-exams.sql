IF OBJECT_ID(N'dbo.ExamLearningResources', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ExamLearningResources(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, ExamId int NOT NULL,
 SectionId int NOT NULL, SubjectId int NOT NULL, Syllabus nvarchar(4000) NOT NULL,
 ResourceUrl nvarchar(1000) NULL, IsPublished bit NOT NULL DEFAULT 1, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE UNIQUE INDEX UX_ExamLearningResources_Scope ON dbo.ExamLearningResources(SchoolId,ExamId,SectionId,SubjectId) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.StudentHallTickets', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.StudentHallTickets(
 Id int IDENTITY(1,1) NOT NULL PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL,
 ExamId int NOT NULL, SeatNumber nvarchar(50) NOT NULL, Room nvarchar(100) NOT NULL,
 DocumentUrl nvarchar(1000) NULL, IsPublished bit NOT NULL DEFAULT 0, IsActive bit NOT NULL DEFAULT 1
 );
 CREATE UNIQUE INDEX UX_StudentHallTickets_Student_Exam ON dbo.StudentHallTickets(StudentId,ExamId) WHERE IsActive=1;
END;
