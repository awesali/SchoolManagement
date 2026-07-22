/* Run on a tested backup first. Backfills EnrollmentId before making it mandatory. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

ALTER TABLE StudentEnrollment ADD
    RollNumber nvarchar(50) NULL,
    AdmissionType nvarchar(30) NOT NULL CONSTRAINT DF_StudentEnrollment_AdmissionType DEFAULT 'New',
    EnrollmentDate datetime2 NOT NULL CONSTRAINT DF_StudentEnrollment_EnrollmentDate DEFAULT SYSUTCDATETIME(),
    PromotionStatus nvarchar(30) NOT NULL CONSTRAINT DF_StudentEnrollment_PromotionStatus DEFAULT 'NotProcessed',
    EnrollmentStatus nvarchar(30) NOT NULL CONSTRAINT DF_StudentEnrollment_EnrollmentStatus DEFAULT 'Active';

UPDATE se SET RollNumber = s.Rollnumber
FROM StudentEnrollment se JOIN Students s ON s.Id = se.StudentId
WHERE se.RollNumber IS NULL;

ALTER TABLE Exams ADD AcademicSessionId int NULL;
ALTER TABLE StudentAttendance ADD EnrollmentId int NULL;
ALTER TABLE ExamMarks ADD EnrollmentId int NULL;
ALTER TABLE ExamResults ADD EnrollmentId int NULL;
ALTER TABLE StudentFees ADD EnrollmentId int NULL;
ALTER TABLE StudentTransportAllocations ADD EnrollmentId int NULL;
ALTER TABLE InventoryStudentOrders ADD EnrollmentId int NULL;

UPDATE e SET AcademicSessionId=x.SessionId
FROM Exams e CROSS APPLY (
 SELECT TOP (1) se.SessionId FROM ExamMarks m JOIN StudentEnrollment se ON se.StudentId=m.StudentId
 WHERE m.ExamId=e.Id AND se.SchoolId=e.SchoolId ORDER BY se.IsActive DESC, se.Created_At DESC
) x WHERE e.AcademicSessionId IS NULL;
UPDATE e SET AcademicSessionId=s.Id
FROM Exams e CROSS APPLY (
 SELECT TOP (1) a.Id FROM AcademicSessions a WHERE a.SchoolId=e.SchoolId ORDER BY a.IsActive DESC, a.Year_Start DESC
) s WHERE e.AcademicSessionId IS NULL;

UPDATE a SET EnrollmentId = se.Id
FROM StudentAttendance a
CROSS APPLY (SELECT TOP (1) e.Id FROM StudentEnrollment e
 WHERE e.StudentId=a.Student_Id AND e.SchoolId=a.School_Id
   AND a.Attendance_Date >= e.Created_At
 ORDER BY e.Created_At DESC, e.Id DESC) se;

UPDATE m SET EnrollmentId = se.Id
FROM ExamMarks m JOIN Exams e ON e.Id=m.ExamId
CROSS APPLY (SELECT TOP (1) x.Id FROM StudentEnrollment x
 WHERE x.StudentId=m.StudentId AND x.SchoolId=m.SchoolId
   AND (e.AcademicSessionId IS NULL OR x.SessionId=e.AcademicSessionId)
 ORDER BY x.IsActive DESC, x.Created_At DESC, x.Id DESC) se;

UPDATE r SET EnrollmentId = se.Id
FROM ExamResults r JOIN Exams e ON e.Id=r.ExamId
CROSS APPLY (SELECT TOP (1) x.Id FROM StudentEnrollment x
 WHERE x.StudentId=r.StudentId AND x.SchoolId=r.SchoolId
   AND (e.AcademicSessionId IS NULL OR x.SessionId=e.AcademicSessionId)
 ORDER BY x.IsActive DESC, x.Created_At DESC, x.Id DESC) se;

UPDATE f SET EnrollmentId=se.Id
FROM StudentFees f JOIN StudentEnrollment se
 ON se.StudentId=f.StudentId AND se.SessionId=f.SessionId AND se.SchoolId=f.SchoolId;
UPDATE t SET EnrollmentId=se.Id
FROM StudentTransportAllocations t JOIN StudentEnrollment se
 ON se.StudentId=t.StudentId AND se.SessionId=t.AcademicSessionId AND se.SchoolId=t.SchoolId;
UPDATE o SET EnrollmentId=se.Id
FROM InventoryStudentOrders o JOIN StudentEnrollment se
 ON se.StudentId=o.StudentId AND se.SessionId=o.AcademicSessionId AND se.SchoolId=o.SchoolId;

CREATE TABLE StudentPromotions (
 Id int IDENTITY PRIMARY KEY, StudentId int NOT NULL, FromEnrollmentId int NOT NULL,
 ToEnrollmentId int NULL, FromSessionId int NOT NULL, ToSessionId int NULL,
 FromClassId int NOT NULL, ToClassId int NULL, FromSectionId int NOT NULL, ToSectionId int NULL,
 PromotionType nvarchar(30) NOT NULL, PromotionDate datetime2 NOT NULL,
 SchoolId int NOT NULL, CreatedBy int NOT NULL, Remarks nvarchar(500) NULL
);

ALTER TABLE StudentAttendance ADD CONSTRAINT FK_StudentAttendance_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE ExamMarks ADD CONSTRAINT FK_ExamMarks_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE ExamResults ADD CONSTRAINT FK_ExamResults_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE StudentFees ADD CONSTRAINT FK_StudentFees_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE StudentTransportAllocations ADD CONSTRAINT FK_TransportAllocation_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE InventoryStudentOrders ADD CONSTRAINT FK_InventoryOrder_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES StudentEnrollment(Id);
ALTER TABLE Exams ALTER COLUMN AcademicSessionId int NOT NULL;
ALTER TABLE Exams ADD CONSTRAINT FK_Exams_AcademicSession FOREIGN KEY (AcademicSessionId) REFERENCES AcademicSessions(Id);

CREATE UNIQUE INDEX UX_StudentEnrollment_Student_Session ON StudentEnrollment(StudentId, SessionId);
CREATE UNIQUE INDEX UX_StudentAttendance_Enrollment_Date ON StudentAttendance(EnrollmentId, Attendance_Date) WHERE EnrollmentId IS NOT NULL;
CREATE UNIQUE INDEX UX_ExamMarks_Enrollment_Schedule ON ExamMarks(EnrollmentId, ExamScheduleId) WHERE EnrollmentId IS NOT NULL;
CREATE UNIQUE INDEX UX_ExamResults_Enrollment_Exam ON ExamResults(EnrollmentId, ExamId) WHERE EnrollmentId IS NOT NULL;
CREATE UNIQUE INDEX UX_StudentFees_Enrollment_Type ON StudentFees(EnrollmentId, FeeTypeId) WHERE EnrollmentId IS NOT NULL;

COMMIT TRANSACTION;
