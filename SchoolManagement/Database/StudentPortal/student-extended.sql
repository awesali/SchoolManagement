IF OBJECT_ID(N'dbo.StudentDiscussionThreads', N'U') IS NULL
CREATE TABLE dbo.StudentDiscussionThreads (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, SectionId int NOT NULL, StaffId int NOT NULL, Title nvarchar(200) NOT NULL, CreatedAt datetime2 NOT NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.StudentDiscussionPosts', N'U') IS NULL
CREATE TABLE dbo.StudentDiscussionPosts (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, ThreadId int NOT NULL, StudentId int NULL, StaffId int NULL, Body nvarchar(2000) NOT NULL, IsApproved bit NOT NULL, CreatedAt datetime2 NOT NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.SchoolClubs', N'U') IS NULL
CREATE TABLE dbo.SchoolClubs (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, Name nvarchar(120) NOT NULL, Description nvarchar(1000) NULL, Schedule nvarchar(200) NULL, Coordinator nvarchar(120) NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.StudentClubMemberships', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentClubMemberships (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, ClubId int NOT NULL, StudentId int NOT NULL, JoinedAt datetime2 NOT NULL, IsActive bit NOT NULL);
CREATE UNIQUE INDEX UX_StudentClubMemberships_Active ON dbo.StudentClubMemberships(ClubId,StudentId) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.StudentEventRegistrations', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentEventRegistrations (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, EventId int NOT NULL, StudentId int NOT NULL, RegisteredAt datetime2 NOT NULL, IsActive bit NOT NULL);
CREATE UNIQUE INDEX UX_StudentEventRegistrations_Active ON dbo.StudentEventRegistrations(EventId,StudentId) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.SchoolLostFoundPosts', N'U') IS NULL
CREATE TABLE dbo.SchoolLostFoundPosts (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, Kind nvarchar(10) NOT NULL, Title nvarchar(160) NOT NULL, Description nvarchar(1000) NOT NULL, IsApproved bit NOT NULL, CreatedAt datetime2 NOT NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.SchoolHouses', N'U') IS NULL
CREATE TABLE dbo.SchoolHouses (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, Name nvarchar(100) NOT NULL, Points int NOT NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.StudentHouseMemberships', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentHouseMemberships (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, HouseId int NOT NULL, IsActive bit NOT NULL);
CREATE UNIQUE INDEX UX_StudentHouseMemberships_Active ON dbo.StudentHouseMemberships(StudentId) WHERE IsActive=1;
END;
IF OBJECT_ID(N'dbo.StudentTransportAlerts', N'U') IS NULL
CREATE TABLE dbo.StudentTransportAlerts (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, StudentId int NULL, Title nvarchar(200) NOT NULL, Message nvarchar(1000) NOT NULL, EffectiveDate datetime2 NOT NULL, CreatedAt datetime2 NOT NULL, IsActive bit NOT NULL);
IF OBJECT_ID(N'dbo.StudentIdentityTokens', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentIdentityTokens (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, Token nvarchar(64) NOT NULL, CreatedAt datetime2 NOT NULL);
CREATE UNIQUE INDEX UX_StudentIdentityTokens_Student ON dbo.StudentIdentityTokens(StudentId);
CREATE UNIQUE INDEX UX_StudentIdentityTokens_Token ON dbo.StudentIdentityTokens(Token);
END;
IF OBJECT_ID(N'dbo.StudentLibraryReservations', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentLibraryReservations (Id int IDENTITY PRIMARY KEY, SchoolId int NOT NULL, StudentId int NOT NULL, BookId int NOT NULL, Status nvarchar(20) NOT NULL, RequestedAt datetime2 NOT NULL, IsActive bit NOT NULL);
CREATE UNIQUE INDEX UX_StudentLibraryReservations_Active ON dbo.StudentLibraryReservations(StudentId,BookId) WHERE IsActive=1 AND Status IN ('Pending','Ready');
END;
