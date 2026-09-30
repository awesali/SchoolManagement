// Backend section: HTTP endpoints and request handling.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/StudentPortal")]
// Exposes student self service HTTP endpoints and handles their requests.
public class StudentSelfServiceController : ControllerBase
{
    // Dependencies and state used by this component.
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    // Creates the component with its required dependencies.
    public StudentSelfServiceController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("overview")]
    // API actions that validate requests and return responses.
    public async Task<IActionResult> Overview()
    {
        if (
            !string.Equals(
                User.FindFirstValue(ClaimTypes.Role),
                "Student",
                StringComparison.OrdinalIgnoreCase
            ) || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)
        )
            return Forbid();

        var credential = await _db
            .Students_Parents_Creds.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == credentialId
                && x.IsActive
                && x.Status == "Active"
                && x.RoleName == "Student"
            );
        if (credential == null)
            return Forbid();

        var matches = await _db
            .Students.AsNoTracking()
            .Where(x =>
                x.SchoolId == credential.School_Id && x.IsActive && x.Email == credential.Email
            )
            .Take(2)
            .ToListAsync();
        if (matches.Count != 1)
            return NotFound(
                new
                {
                    message = "No unique student record is linked to this login. Contact your school administrator.",
                }
            );
        var student = matches[0];
        var enrollment = await _db
            .StudentEnrollment.AsNoTracking()
            .Where(x =>
                x.StudentId == student.Id
                && x.SchoolId == student.SchoolId
                && x.IsActive
                && x.EnrollmentStatus == "Active"
            )
            .OrderByDescending(x => x.EnrollmentDate)
            .FirstOrDefaultAsync();
        if (enrollment == null)
            return NotFound(new { message = "No active enrollment was found for this student." });

        var school = await _db
            .Schools.AsNoTracking()
            .Where(x => x.Id == student.SchoolId)
            .Select(x => new { x.SchoolName, x.Address })
            .FirstOrDefaultAsync();
        var schoolLogoUrl = await _db
            .ProfilePictures.AsNoTracking()
            .Where(x => x.PersonType == "School" && x.PersonId == student.SchoolId && x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => x.FileUrl)
            .FirstOrDefaultAsync();
        var profilePictureUrl = await _db
            .ProfilePictures.AsNoTracking()
            .Where(x => x.PersonType == "Student" && x.PersonId == student.Id && x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => x.FileUrl)
            .FirstOrDefaultAsync();
        var className = await _db
            .Classes.AsNoTracking()
            .Where(x => x.Id == enrollment.ClassId)
            .Select(x => x.ClassName)
            .FirstOrDefaultAsync();
        var sectionName = await _db
            .SectionDetails.AsNoTracking()
            .Where(x => x.Id == enrollment.SectionId)
            .Select(x => x.SectionName)
            .FirstOrDefaultAsync();
        var timetable = await (
            from slot in _db.Timetables.AsNoTracking()
            join period in _db.TimetablePeriods on slot.PeriodId equals period.Id
            join subject in _db.Subjects on slot.SubjectId equals subject.Id into subjectJoin
            from subject in subjectJoin.DefaultIfEmpty()
            where
                slot.SectionId == enrollment.SectionId
                && slot.SchoolId == student.SchoolId
                && slot.IsActive
            orderby slot.DayOfWeek, period.PeriodNumber
            select new
            {
                slot.Id,
                slot.DayOfWeek,
                period.PeriodNumber,
                period.StartTime,
                period.EndTime,
                period.IsBreak,
                updatedDate = slot.Updated_Date,
                SubjectName = subject == null ? "Break" : subject.SubjectName,
            }
        ).ToListAsync();
        var homework = await (
            from item in _db.HomeworkAssignments.AsNoTracking()
            join subject in _db.Subjects on item.SubjectId equals subject.Id
            where
                item.SectionId == enrollment.SectionId
                && item.SchoolId == student.SchoolId
                && item.IsActive
                && item.Status == "Published"
            orderby item.DueDate
            select new
            {
                item.Id,
                item.Title,
                item.Description,
                item.AssignedDate,
                item.DueDate,
                item.TotalMarks,
                item.ResourceUrl,
                subject.SubjectName,
            }
        ).ToListAsync();
        var materials = await (
            from item in _db.TeacherStudyMaterials.AsNoTracking()
            join subject in _db.Subjects on item.SubjectId equals subject.Id
            where
                item.SectionId == enrollment.SectionId
                && item.SchoolId == student.SchoolId
                && item.IsActive
            orderby item.CreatedDate descending
            select new
            {
                item.Id,
                item.Title,
                item.Description,
                item.ResourceType,
                item.ResourceUrl,
                item.CreatedDate,
                subject.SubjectName,
            }
        ).ToListAsync();
        var attendance = await _db
            .StudentAttendance.AsNoTracking()
            .Where(x =>
                x.Student_Id == student.Id
                && x.EnrollmentId == enrollment.Id
                && x.School_Id == student.SchoolId
                && x.IsActive
            )
            .OrderByDescending(x => x.Attendance_Date)
            .Select(x => new { date = x.Attendance_Date, x.Status })
            .ToListAsync();
        var exams = await (
            from schedule in _db.ExamSchedules.AsNoTracking()
            join exam in _db.Exams on schedule.ExamId equals exam.Id
            join examTypeRecord in _db.ExamTypes
                on exam.ExamTypeId equals examTypeRecord.Id
                into examTypeJoin
            from examType in examTypeJoin.DefaultIfEmpty()
            join subject in _db.Subjects on schedule.SubjectId equals subject.Id into subjectJoin
            from subject in subjectJoin.DefaultIfEmpty()
            where
                schedule.SectionId == enrollment.SectionId
                && schedule.ClassId == enrollment.ClassId
                && schedule.SchoolId == student.SchoolId
                && schedule.IsActive
                && exam.IsActive
                && exam.IsPublished
                && exam.AcademicSessionId == enrollment.SessionId
            orderby schedule.ExamDate
            select new
            {
                schedule.Id,
                examId = exam.Id,
                examName = exam.Name,
                examTypeName = examType == null ? "" : examType.Name,
                createdDate = exam.CreatedDate,
                schedule.ExamDate,
                StartTime = (TimeSpan?)schedule.StartTime,
                EndTime = (TimeSpan?)schedule.EndTime,
                subjectName = subject == null ? "" : subject.SubjectName,
            }
        ).ToListAsync();
        // Unit tests are dated when teachers create them but have no ExamSchedule until a time is set.
        // Include these published tests so students can see them and receive the exam notification.
        var unscheduledUnitTests = await (
            from item in _db.ExamSubjects.AsNoTracking()
            join exam in _db.Exams on item.ExamId equals exam.Id
            join examType in _db.ExamTypes on exam.ExamTypeId equals examType.Id
            join subject in _db.Subjects on item.SubjectId equals subject.Id
            where
                item.SectionId == enrollment.SectionId
                && item.ClassId == enrollment.ClassId
                && item.SchoolId == student.SchoolId
                && item.IsActive
                && exam.SchoolId == student.SchoolId
                && exam.AcademicSessionId == enrollment.SessionId
                && exam.IsActive
                && exam.IsPublished
                && exam.StartDate.HasValue
                && examType.schoolId == student.SchoolId
                && examType.Name.ToLower() == "unit test"
                && !_db.ExamSchedules.Any(schedule =>
                    schedule.ExamId == exam.Id
                    && schedule.SectionId == enrollment.SectionId
                    && schedule.IsActive
                )
            orderby exam.StartDate
            select new
            {
                Id = -item.Id,
                examId = exam.Id,
                examName = exam.Name,
                examTypeName = examType.Name,
                createdDate = exam.CreatedDate,
                ExamDate = exam.StartDate.Value,
                StartTime = (TimeSpan?)null,
                EndTime = (TimeSpan?)null,
                subjectName = subject.SubjectName,
            }
        ).ToListAsync();
        exams.AddRange(unscheduledUnitTests);
        exams = exams.OrderBy(exam => exam.ExamDate).ToList();
        var results = await (
            from result in _db.ExamResults.AsNoTracking()
            join exam in _db.Exams on result.ExamId equals exam.Id
            where
                result.StudentId == student.Id
                && result.EnrollmentId == enrollment.Id
                && result.SchoolId == student.SchoolId
                && result.Published
                && exam.ResultPublished
                && exam.IsActive
            select new
            {
                examId = exam.Id,
                examName = exam.Name,
                createdDate = result.Created_Date,
                result.TotalMarks,
                result.ObtainedMarks,
                result.Percentage,
                result.Grade,
                result.ResultStatus,
            }
        ).ToListAsync();
        var parent = await _db
            .ParentDetails.AsNoTracking()
            .Where(x => x.Id == student.ParentId && x.IsActive)
            .Select(x => new
            {
                x.Name,
                x.Relationship,
                x.Email,
                x.PhoneNumber,
            })
            .FirstOrDefaultAsync();
        var teachers = await (
            from mapping in _db.SectionSubjectTeachers.AsNoTracking()
            join staff in _db.Staff on mapping.StaffId equals staff.Id
            join subject in _db.Subjects on mapping.SubjectId equals subject.Id
            where
                mapping.SectionId == enrollment.SectionId
                && mapping.SchoolId == student.SchoolId
                && mapping.IsActive
                && staff.IsActive
                && subject.IsActive
            orderby subject.SubjectName
            select new
            {
                staff.Id,
                staff.Name,
                subject.SubjectName,
            }
        ).ToListAsync();
        var documents = await _db
            .Student_Documents.AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new
            {
                x.Id,
                x.DocumentName,
                x.FileName,
                x.FileUrl,
                x.CreatedDate,
            })
            .ToListAsync();
        var fees = await (
            from fee in _db.StudentFees.AsNoTracking()
            join type in _db.FeeTypes on fee.FeeTypeId equals type.Id
            where
                fee.StudentId == student.Id
                && fee.EnrollmentId == enrollment.Id
                && fee.SchoolId == student.SchoolId
                && fee.IsActive
            orderby type.Name
            select new
            {
                fee.Id,
                fee.Amount,
                fee.Status,
                feeType = type.Name,
            }
        ).ToListAsync();
        var feeIds = fees.Select(x => x.Id).ToList();
        var payments = await _db
            .FeePayments.AsNoTracking()
            .Where(x =>
                feeIds.Contains(x.StudentFeeId) && x.SchoolId == student.SchoolId && x.IsActive
            )
            .OrderByDescending(x => x.Payment_Date)
            .Select(x => new
            {
                x.Id,
                x.StudentFeeId,
                x.AmountPaid,
                x.Payment_Date,
                x.Payment_Mode,
                x.Receipt_Number,
            })
            .ToListAsync();
        var transport = await (
            from allocation in _db.StudentTransportAllocations.AsNoTracking()
            join assignment in _db.TransportVehicleAssignments
                on allocation.VehicleAssignmentId equals assignment.Id
            join route in _db.TransportRoutes on assignment.RouteId equals route.Id
            join vehicle in _db.TransportVehicles on assignment.VehicleId equals vehicle.Id
            where
                allocation.StudentId == student.Id
                && allocation.AcademicSessionId == enrollment.SessionId
                && allocation.SchoolId == student.SchoolId
                && allocation.IsActive
                && assignment.IsActive
            select new
            {
                route.RouteName,
                vehicle.VehicleNumber,
                allocation.PickupStop,
                allocation.DropStop,
                allocation.SeatNumber,
                allocation.PickupShift,
                allocation.DropShift,
                allocation.MonthlyFee,
                allocation.FeeType,
                allocation.DueDate,
            }
        ).FirstOrDefaultAsync();
        var transportFees = await (
            from fee in _db.TransportFees.AsNoTracking()
            join allocation in _db.StudentTransportAllocations.AsNoTracking()
                on fee.StudentTransportAllocationId equals allocation.Id
            where
                allocation.StudentId == student.Id
                && allocation.SchoolId == student.SchoolId
                && allocation.AcademicSessionId == enrollment.SessionId
                && fee.SchoolId == student.SchoolId
            orderby fee.FeeYear descending, fee.FeeMonth descending
            select new
            {
                fee.Id,
                fee.FeeMonth,
                fee.FeeYear,
                fee.Amount,
                fee.PaidAmount,
                fee.Status,
                fee.DueDate,
            }
        ).ToListAsync();
        var transportFeeIds = transportFees.Select(x => x.Id).ToList();
        var transportPayments = await _db
            .TransportFeePayments.AsNoTracking()
            .Where(x => transportFeeIds.Contains(x.TransportFeeId))
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.TransportFeeId,
                x.Amount,
                x.PaymentDate,
                x.PaymentMode,
                x.ReceiptNumber,
                x.ReferenceNumber,
            })
            .ToListAsync();
        var diary = await (
            from entry in _db.ClassDiaryEntries.AsNoTracking()
            join subject in _db.Subjects on entry.SubjectId equals subject.Id
            join staff in _db.Staff on entry.StaffId equals staff.Id
            where
                entry.SchoolId == student.SchoolId
                && entry.SectionId == enrollment.SectionId
                && entry.IsActive
                && entry.IsPublished
            orderby entry.EntryDate descending
            select new
            {
                entry.Id,
                entry.EntryDate,
                entry.Topic,
                entry.Pages,
                entry.Homework,
                subject.SubjectName,
                teacherName = staff.Name,
            }
        )
            .Take(100)
            .ToListAsync();
        var submissions = await _db
            .AssignmentSubmissions.AsNoTracking()
            .Where(x =>
                x.StudentId == student.Id
                && x.EnrollmentId == enrollment.Id
                && x.SchoolId == student.SchoolId
                && x.IsActive
            )
            .Select(x => new
            {
                x.Id,
                x.AssignmentId,
                x.SubmittedAt,
                x.TextAnswer,
                x.Status,
                x.TeacherFeedback,
                x.Marks,
                hasFile = x.FileUrl != null,
            })
            .ToListAsync();
        var announcements = await _db
            .SchoolAnnouncements.AsNoTracking()
            .Where(x =>
                x.SchoolId == student.SchoolId
                && x.IsActive
                && x.IsPublished
                && (x.SectionId == null || x.SectionId == enrollment.SectionId)
                && (x.ExpiresAt == null || x.ExpiresAt.Value.Date >= DateTime.Today)
            )
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Body,
                x.CreatedAt,
                x.IsPinned,
            })
            .Take(50)
            .ToListAsync();
        var libraryBooks = await _db
            .InventoryBooks.AsNoTracking()
            .Where(x =>
                x.SchoolId == student.SchoolId
                && x.AcademicSessionId == enrollment.SessionId
                && (x.ClassId == null || x.ClassId == enrollment.ClassId)
                && (x.SectionId == null || x.SectionId == enrollment.SectionId)
            )
            .OrderBy(x => x.BookName)
            .Select(x => new
            {
                x.Id,
                x.BookName,
                x.Publisher,
                x.Edition,
                x.Isbn,
                x.SubjectId,
            })
            .Take(200)
            .ToListAsync();
        var borrowedBooks = await (
            from order in _db.InventoryStudentOrders.AsNoTracking()
            join item in _db.InventoryStudentOrderItems on order.Id equals item.StudentOrderId
            join product in _db.InventoryProducts on item.ProductId equals product.Id
            where
                order.StudentId == student.Id
                && order.EnrollmentId == enrollment.Id
                && order.SchoolId == student.SchoolId
                && order.OrderType == "Borrow"
            select new
            {
                title = product.ProductName,
                order.OrderNumber,
                order.BorrowDateTime,
                order.ReturnDateTime,
                order.Status,
            }
        ).ToListAsync();

        var requests = await _db
            .StudentServiceRequests.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Subject,
                x.Details,
                x.FromDate,
                x.ToDate,
                x.Status,
                x.Response,
                x.CreatedAt,
                x.RespondedAt,
                x.RecipientRoleId,
                x.RecipientUserId,
            })
            .ToListAsync();
        var messages = await (
            from message in _db.TeacherStudentMessages.AsNoTracking()
            join staff in _db.Staff on message.StaffId equals staff.Id
            where
                message.SchoolId == student.SchoolId
                && message.StudentId == student.Id
                && message.IsActive
            orderby message.SentAt
            select new
            {
                message.Id,
                message.StaffId,
                staffName = staff.Name,
                staff.RoleId,
                message.Body,
                message.FromStudent,
                message.SentAt,
                message.ReadAt,
            }
        ).ToListAsync();
        var achievements = await _db
            .StudentAchievements.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .OrderByDescending(x => x.AwardedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.AwardedAt,
            })
            .ToListAsync();
        var schoolEvents = await _db
            .SchoolCalendarEvents.AsNoTracking()
            .Where(x =>
                x.SchoolId == student.SchoolId
                && x.IsActive
                && (x.SectionId == null || x.SectionId == enrollment.SectionId)
            )
            .OrderBy(x => x.EventDate)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.EventDate,
                x.EndDate,
                x.EventType,
            })
            .ToListAsync();

        var publishedExamIds = results.Select(x => x.examId).Distinct().ToList();
        var markRows = await (
            from mark in _db.ExamMarks.AsNoTracking()
            join schedule in _db.ExamSchedules on mark.ExamScheduleId equals schedule.Id
            join exam in _db.Exams on mark.ExamId equals exam.Id
            join subject in _db.Subjects on schedule.SubjectId equals subject.Id
            where
                mark.StudentId == student.Id
                && mark.EnrollmentId == enrollment.Id
                && mark.SchoolId == student.SchoolId
                && mark.IsActive
                && exam.ResultPublished
                && exam.IsActive
                && publishedExamIds.Contains(exam.Id)
            orderby mark.EnteredDate descending
            select new
            {
                examId = exam.Id,
                examName = exam.Name,
                subjectId = subject.Id,
                subjectName = subject.SubjectName,
                mark.ObtainedMarks,
                mark.Remarks,
                mark.EnteredDate,
            }
        ).ToListAsync();
        var examIds = publishedExamIds;
        var maxMarks = await _db
            .ExamSubjects.AsNoTracking()
            .Where(x =>
                x.SchoolId == student.SchoolId
                && x.ClassId == enrollment.ClassId
                && (x.SectionId == null || x.SectionId == enrollment.SectionId)
                && examIds.Contains(x.ExamId)
                && x.IsActive
            )
            .Select(x => new
            {
                x.ExamId,
                x.SubjectId,
                x.SectionId,
                x.MaxMarks,
            })
            .ToListAsync();
        var gradeHistory = markRows
            .Select(row => new
            {
                row.examId,
                row.examName,
                row.subjectName,
                row.ObtainedMarks,
                maxMarks = maxMarks
                    .Where(x => x.ExamId == row.examId && x.SubjectId == row.subjectId)
                    .OrderByDescending(x => x.SectionId == enrollment.SectionId)
                    .Select(x => (decimal?)x.MaxMarks)
                    .FirstOrDefault(),
                row.Remarks,
                row.EnteredDate,
            })
            .ToList();

        var scheduledResultSubjects = await (
            from schedule in _db.ExamSchedules.AsNoTracking()
            join subject in _db.Subjects.AsNoTracking() on schedule.SubjectId equals subject.Id
            where
                schedule.SchoolId == student.SchoolId
                && schedule.ClassId == enrollment.ClassId
                && schedule.SectionId == enrollment.SectionId
                && schedule.IsActive
                && examIds.Contains(schedule.ExamId)
            select new
            {
                examId = schedule.ExamId,
                subjectId = subject.Id,
                subjectName = subject.SubjectName,
            }
        ).ToListAsync();
        var resultSubjects = scheduledResultSubjects
            .GroupBy(x => new { x.examId, x.subjectId })
            .Select(group =>
            {
                var subject = group.First();
                return new
                {
                    subject.examId,
                    subject.subjectId,
                    subject.subjectName,
                    maxMarks = maxMarks
                        .Where(x => x.ExamId == subject.examId && x.SubjectId == subject.subjectId)
                        .OrderByDescending(x => x.SectionId == enrollment.SectionId)
                        .Select(x => (decimal?)x.MaxMarks)
                        .FirstOrDefault(),
                    obtainedMarks = markRows
                        .Where(x => x.examId == subject.examId && x.subjectId == subject.subjectId)
                        .Select(x => (decimal?)x.ObtainedMarks)
                        .FirstOrDefault(),
                };
            })
            .OrderBy(x => x.subjectName)
            .ToList();
        var resultDetails = results
            .Select(result =>
            {
                var rows = resultSubjects.Where(x => x.examId == result.examId).ToList();
                var configuredTotal = rows.All(x => x.maxMarks > 0)
                    ? (decimal?)rows.Sum(x => x.maxMarks ?? 0)
                    : null;
                var recordedObtained = rows.Sum(x => x.obtainedMarks ?? 0);
                var isComplete =
                    rows.Count > 0
                    && configuredTotal.HasValue
                    && rows.All(x => x.obtainedMarks.HasValue)
                    && result.TotalMarks == configuredTotal.Value
                    && result.ObtainedMarks == recordedObtained;
                return new
                {
                    result.examId,
                    result.examName,
                    result.createdDate,
                    result.TotalMarks,
                    result.ObtainedMarks,
                    result.Percentage,
                    result.Grade,
                    result.ResultStatus,
                    expectedSubjectCount = rows.Count,
                    recordedSubjectCount = rows.Count(x => x.obtainedMarks.HasValue),
                    configuredTotalMarks = configuredTotal,
                    recordedObtainedMarks = recordedObtained,
                    isComplete,
                };
            })
            .ToList();
        var examResources = await (
            from resource in _db.ExamLearningResources.AsNoTracking()
            join exam in _db.Exams on resource.ExamId equals exam.Id
            join subject in _db.Subjects on resource.SubjectId equals subject.Id
            where
                resource.SchoolId == student.SchoolId
                && resource.SectionId == enrollment.SectionId
                && resource.IsActive
                && resource.IsPublished
                && exam.IsActive
                && exam.IsPublished
                && exam.AcademicSessionId == enrollment.SessionId
            select new
            {
                resource.Id,
                resource.ExamId,
                examName = exam.Name,
                subjectName = subject.SubjectName,
                resource.Syllabus,
                resource.ResourceUrl,
            }
        ).ToListAsync();
        var hallTickets = await (
            from ticket in _db.StudentHallTickets.AsNoTracking()
            join exam in _db.Exams on ticket.ExamId equals exam.Id
            where
                ticket.SchoolId == student.SchoolId
                && ticket.StudentId == student.Id
                && ticket.IsActive
                && ticket.IsPublished
                && exam.IsActive
                && exam.IsPublished
            select new
            {
                ticket.Id,
                ticket.ExamId,
                examName = exam.Name,
                createdAt = exam.CreatedDate,
                ticket.SeatNumber,
                ticket.Room,
                ticket.Venue,
                ticket.DocumentUrl,
            }
        ).ToListAsync();
        return Ok(
            new
            {
                success = true,
                data = new
                {
                    profile = new
                    {
                        student.StudentName,
                        student.Email,
                        rollNumber = enrollment.RollNumber ?? student.Rollnumber,
                        schoolName = school?.SchoolName,
                        schoolAddress = school?.Address,
                        schoolLogoUrl,
                        profilePictureUrl,
                        className,
                        sectionName,
                    },
                    timetable,
                    homework,
                    materials,
                    attendance,
                    exams,
                    results = resultDetails,
                    resultSubjects,
                    parent,
                    teachers,
                    documents,
                    fees,
                    payments,
                    transport,
                    transportFees,
                    transportPayments,
                    diary,
                    submissions,
                    announcements,
                    libraryBooks,
                    borrowedBooks,
                    requests,
                    messages,
                    achievements,
                    schoolEvents,
                    gradeHistory,
                    examResources,
                    hallTickets,
                },
            }
        );
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] SchoolManagement.DTOs.ChangePasswordDto input
    )
    {
        if (
            !User.IsInRole("Student")
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)
        )
            return Forbid();
        var credential = await _db.Students_Parents_Creds.FirstOrDefaultAsync(x =>
            x.Id == credentialId && x.IsActive && x.Status == "Active" && x.RoleName == "Student"
        );
        if (credential == null)
            return Forbid();
        if (string.IsNullOrEmpty(input.CurrentPassword))
            return BadRequest(new { success = false, message = "Current password is required." });
        if (
            string.IsNullOrEmpty(input.NewPassword)
            || input.NewPassword.Length < 8
            || input.NewPassword.Length > 128
        )
            return BadRequest(
                new { success = false, message = "New password must contain 8 to 128 characters." }
            );
        if (input.NewPassword != input.ConfirmPassword)
            return BadRequest(
                new { success = false, message = "New password and confirmation do not match." }
            );
        if (input.CurrentPassword == input.NewPassword)
            return BadRequest(
                new
                {
                    success = false,
                    message = "New password must be different from the current password.",
                }
            );
        if (!BCrypt.Net.BCrypt.Verify(input.CurrentPassword, credential.Password_Hash))
            return BadRequest(new { success = false, message = "Current password is incorrect." });
        credential.Password_Hash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Password changed successfully." });
    }

    private async Task<(
        SchoolManagement.Model.Students? student,
        SchoolManagement.Model.StudentEnrollment? enrollment
    )> CurrentEnrollment()
    {
        if (
            !User.IsInRole("Student")
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)
        )
            return (null, null);
        var credential = await _db
            .Students_Parents_Creds.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == credentialId
                && x.IsActive
                && x.RoleName == "Student"
                && x.Status == "Active"
            );
        if (credential == null)
            return (null, null);
        var matches = await _db
            .Students.AsNoTracking()
            .Where(x =>
                x.SchoolId == credential.School_Id && x.Email == credential.Email && x.IsActive
            )
            .Take(2)
            .ToListAsync();
        if (matches.Count != 1)
            return (null, null);
        var student = matches[0];
        var enrollment = await _db
            .StudentEnrollment.AsNoTracking()
            .Where(x =>
                x.StudentId == student.Id
                && x.SchoolId == student.SchoolId
                && x.IsActive
                && x.EnrollmentStatus == "Active"
            )
            .OrderByDescending(x => x.EnrollmentDate)
            .FirstOrDefaultAsync();
        return (student, enrollment);
    }

    [HttpPost("submissions")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] StudentSubmissionInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        var assignment = await _db
            .HomeworkAssignments.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == input.AssignmentId
                && x.SchoolId == student.SchoolId
                && x.SectionId == enrollment.SectionId
                && x.IsActive
                && x.Status == "Published"
            );
        if (assignment == null)
            return NotFound(new { message = "Assignment is not available for your class." });
        var answer = input.TextAnswer?.Trim();
        if (string.IsNullOrWhiteSpace(answer) && input.File == null)
            return BadRequest(new { message = "Add an answer or upload a file." });
        var existing = await _db.AssignmentSubmissions.FirstOrDefaultAsync(x =>
            x.AssignmentId == assignment.Id && x.StudentId == student.Id && x.IsActive
        );
        if (existing != null && existing.Status != "Resubmission Required")
            return Conflict(new { message = "You already submitted this assignment." });
        string? storedFile = null;
        if (input.File != null)
        {
            var allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = new[] { "application/pdf" },
                [".doc"] = new[] { "application/msword" },
                [".docx"] = new[]
                {
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                },
                [".jpg"] = new[] { "image/jpeg" },
                [".jpeg"] = new[] { "image/jpeg" },
                [".png"] = new[] { "image/png" },
                [".webp"] = new[] { "image/webp" },
            };
            var extension = Path.GetExtension(input.File.FileName).ToLowerInvariant();
            if (
                input.File.Length == 0
                || input.File.Length > 10 * 1024 * 1024
                || !allowed.TryGetValue(extension, out var types)
                || !types.Contains(input.File.ContentType)
            )
                return BadRequest(
                    new { message = "Upload a PDF, Word document or image up to 10 MB." }
                );
            var folder = Path.Combine(
                _env.ContentRootPath,
                "private-uploads",
                "student-submissions"
            );
            Directory.CreateDirectory(folder);
            storedFile = Guid.NewGuid().ToString("N") + extension;
            await using var stream = new FileStream(
                Path.Combine(folder, storedFile),
                FileMode.CreateNew
            );
            await input.File.CopyToAsync(stream);
        }
        var status = assignment.DueDate < DateTime.Now ? "Late" : "Submitted";
        if (existing == null)
            _db.AssignmentSubmissions.Add(
                new SchoolManagement.Model.AssignmentSubmission
                {
                    SchoolId = student.SchoolId,
                    AssignmentId = assignment.Id,
                    StudentId = student.Id,
                    EnrollmentId = enrollment.Id,
                    TextAnswer = answer,
                    FileUrl = storedFile,
                    SubmittedAt = DateTime.UtcNow,
                    Status = status,
                }
            );
        else
        {
            existing.TextAnswer = answer;
            existing.FileUrl = storedFile ?? existing.FileUrl;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.Status = status;
            existing.Marks = null;
            existing.TeacherFeedback = null;
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Assignment submitted." });
    }

    [HttpGet("submissions/{id:int}/file")]
    public async Task<IActionResult> DownloadSubmissionFile(int id)
    {
        var (student, _) = await CurrentEnrollment();
        if (student == null)
            return Forbid();
        var submission = await _db
            .AssignmentSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id
                && x.StudentId == student.Id
                && x.SchoolId == student.SchoolId
                && x.IsActive
            );
        if (submission?.FileUrl == null)
            return NotFound();
        var path = Path.Combine(
            _env.ContentRootPath,
            "private-uploads",
            "student-submissions",
            Path.GetFileName(submission.FileUrl)
        );
        if (!System.IO.File.Exists(path))
            return NotFound();
        return PhysicalFile(
            path,
            "application/octet-stream",
            "submission" + Path.GetExtension(path)
        );
    }

    [HttpGet("request-recipients")]
    public async Task<IActionResult> RequestRecipients()
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        var roles = await _db
            .Roles.AsNoTracking()
            .Where(x =>
                x.IsActive && x.Id != 1 && (x.School_Id == null || x.School_Id == student.SchoolId)
            )
            .OrderBy(x => x.RoleName)
            .Select(x => new { x.Id, x.RoleName })
            .ToListAsync();
        var recipients = await (
            from account in _db.Users.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on account.RoleId equals role.Id
            join staff in _db.Staff.AsNoTracking() on account.Id equals staff.usersid into staffJoin
            from staff in staffJoin.DefaultIfEmpty()
            where
                account.IsActive
                && account.Status
                && role.IsActive
                && role.Id != 1
                && (role.School_Id == null || role.School_Id == student.SchoolId)
                && (
                    (
                        staff != null
                        && staff.SchoolId == student.SchoolId
                        && staff.IsActive
                        && staff.RoleId == account.RoleId
                    ) || (account.School_Id == student.SchoolId && staff == null)
                )
            orderby role.RoleName, account.Name
            select new
            {
                id = account.Id,
                name = account.Name,
                roleId = account.RoleId,
            }
        )
            .Distinct()
            .ToListAsync();
        return Ok(new { success = true, data = new { roles, recipients } });
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] StudentRequestInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        var allowed = new[]
        {
            "Leave",
            "Certificate",
            "ID Card",
            "General",
            "Helpdesk",
            "Transport",
            "Lost & Found",
        };
        if (
            !allowed.Contains(input.Type)
            || string.IsNullOrWhiteSpace(input.Subject)
            || string.IsNullOrWhiteSpace(input.Details)
            || input.Subject.Length > 200
            || input.Details.Length > 2000
        )
            return BadRequest(
                new { message = "Choose a request type and enter a subject and details." }
            );
        if (
            input.Type == "Leave"
            && (
                !input.FromDate.HasValue
                || !input.ToDate.HasValue
                || input.FromDate.Value.Date < DateTime.Today
                || input.ToDate.Value.Date < input.FromDate.Value.Date
            )
        )
            return BadRequest(new { message = "Choose a valid future leave date range." });
        if (
            !input.RecipientRoleId.HasValue
            || !input.RecipientUserId.HasValue
            || input.RecipientRoleId.Value == 1
        )
            return BadRequest(new { message = "Choose a recipient role and staff member." });
        var recipient = await (
            from account in _db.Users.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on account.RoleId equals role.Id
            join staff in _db.Staff.AsNoTracking() on account.Id equals staff.usersid into staffJoin
            from staff in staffJoin.DefaultIfEmpty()
            where
                account.Id == input.RecipientUserId.Value
                && account.IsActive
                && account.Status
                && role.IsActive
                && role.Id != 1
                && (role.School_Id == null || role.School_Id == student.SchoolId)
                && account.RoleId == input.RecipientRoleId.Value
                && (
                    staff != null
                        && staff.SchoolId == student.SchoolId
                        && staff.IsActive
                        && staff.RoleId == account.RoleId
                    || account.School_Id == student.SchoolId && staff == null
                )
            select account.Id
        ).AnyAsync();
        if (!recipient)
            return BadRequest(
                new { message = "The selected staff member is not active in your school and role." }
            );
        var request = new SchoolManagement.Model.StudentServiceRequest
        {
            SchoolId = student.SchoolId,
            StudentId = student.Id,
            EnrollmentId = enrollment.Id,
            RecipientRoleId = input.RecipientRoleId,
            RecipientUserId = input.RecipientUserId,
            Type = input.Type,
            Subject = input.Subject.Trim(),
            Details = input.Details.Trim(),
            FromDate = input.FromDate?.Date,
            ToDate = input.ToDate?.Date,
        };
        _db.StudentServiceRequests.Add(request);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { request.Id } });
    }

    [HttpGet("message-recipients")]
    public async Task<IActionResult> MessageRecipients()
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        var roles = await _db
            .Roles.AsNoTracking()
            .Where(x =>
                x.IsActive
                && x.Id != 1
                && x.Id != 7
                && (x.School_Id == null || x.School_Id == student.SchoolId)
            )
            .OrderBy(x => x.RoleName)
            .Select(x => new { x.Id, x.RoleName })
            .ToListAsync();
        var staff = await (
            from person in _db.Staff.AsNoTracking()
            join account in _db.Users.AsNoTracking() on person.usersid equals account.Id
            join role in _db.Roles.AsNoTracking() on person.RoleId equals role.Id
            where
                person.SchoolId == student.SchoolId
                && person.IsActive
                && account.IsActive
                && account.Status
                && account.RoleId == person.RoleId
                && role.IsActive
                && role.Id != 1
                && role.Id != 7
                && (role.School_Id == null || role.School_Id == student.SchoolId)
            orderby role.RoleName, person.Name
            select new
            {
                id = person.Id,
                name = person.Name,
                roleId = person.RoleId,
            }
        ).ToListAsync();
        return Ok(new { success = true, data = new { roles, staff } });
    }

    [HttpGet("messages")]
    public async Task<IActionResult> Messages()
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        var rows = await (
            from message in _db.TeacherStudentMessages.AsNoTracking()
            join staff in _db.Staff.AsNoTracking() on message.StaffId equals staff.Id
            where
                message.SchoolId == student.SchoolId
                && message.StudentId == student.Id
                && staff.SchoolId == student.SchoolId
                && message.IsActive
            orderby message.SentAt, message.Id
            select new
            {
                message.Id,
                message.StaffId,
                staffName = staff.Name,
                staff.RoleId,
                message.Body,
                message.FromStudent,
                message.SentAt,
                message.ReadAt,
            }
        ).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("messages/{staffId:int}/read")]
    public async Task<IActionResult> ReadMessages(int staffId)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        await _db
            .TeacherStudentMessages.Where(x =>
                x.SchoolId == student.SchoolId
                && x.StudentId == student.Id
                && x.StaffId == staffId
                && x.IsActive
                && !x.FromStudent
                && x.ReadAt == null
            )
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, DateTime.UtcNow));
        return Ok(new { success = true });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] StudentMessageInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null)
            return Forbid();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000)
            return BadRequest(new { message = "Enter a message up to 2000 characters." });
        if (input.RecipientRoleId <= 1 || input.RecipientRoleId == 7 || input.StaffId <= 0)
            return BadRequest(new { message = "Choose a role and staff member." });
        var recipient = await (
            from staff in _db.Staff.AsNoTracking()
            join account in _db.Users.AsNoTracking() on staff.usersid equals account.Id
            join role in _db.Roles.AsNoTracking() on staff.RoleId equals role.Id
            where
                staff.Id == input.StaffId
                && staff.SchoolId == student.SchoolId
                && staff.IsActive
                && staff.RoleId == input.RecipientRoleId
                && account.RoleId == staff.RoleId
                && account.IsActive
                && account.Status
                && role.IsActive
                && role.Id != 1
                && role.Id != 7
                && (role.School_Id == null || role.School_Id == student.SchoolId)
            select staff.Id
        ).AnyAsync();
        if (!recipient)
            return BadRequest(new { message = "The selected staff member is unavailable." });
        var message = new SchoolManagement.Model.TeacherStudentMessage
        {
            SchoolId = student.SchoolId,
            StudentId = student.Id,
            StaffId = input.StaffId,
            Body = input.Body.Trim(),
            FromStudent = true,
        };
        _db.TeacherStudentMessages.Add(message);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { message.Id } });
    }
}

public class StudentSubmissionInput
{
    public int AssignmentId { get; set; }
    public string? TextAnswer { get; set; }
    public IFormFile? File { get; set; }
}

public class StudentRequestInput
{
    public int? RecipientRoleId { get; set; }
    public int? RecipientUserId { get; set; }
    public string Type { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class StudentMessageInput
{
    public int RecipientRoleId { get; set; }
    public int StaffId { get; set; }
    public string Body { get; set; } = "";
}
