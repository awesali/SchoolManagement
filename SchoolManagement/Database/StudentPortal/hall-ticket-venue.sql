IF OBJECT_ID(N'dbo.StudentHallTickets', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StudentHallTickets', N'Venue') IS NULL
BEGIN
    ALTER TABLE dbo.StudentHallTickets ADD Venue nvarchar(200) NULL;
END;
