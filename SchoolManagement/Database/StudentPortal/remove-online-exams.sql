-- Remove the retired online-exam question bank and attempts.
-- Regular Exams, ExamSchedules, ExamResults, ExamLearningResources, and StudentHallTickets remain intact.
IF OBJECT_ID(N'dbo.OnlineExamAttempts', N'U') IS NOT NULL
    DROP TABLE dbo.OnlineExamAttempts;
IF OBJECT_ID(N'dbo.OnlineExamQuestions', N'U') IS NOT NULL
    DROP TABLE dbo.OnlineExamQuestions;
