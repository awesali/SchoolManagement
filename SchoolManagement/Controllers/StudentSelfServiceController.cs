using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/StudentPortal")]
public class StudentSelfServiceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public StudentSelfServiceController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Student", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)) return Forbid();

        var credential = await _db.Students_Parents_Creds.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == credentialId && x.IsActive && x.Status == "Active" && x.RoleName == "Student");
        if (credential == null) return Forbid();

        var matches = await _db.Students.AsNoTracking()
            .Where(x => x.SchoolId == credential.School_Id && x.IsActive && x.Email == credential.Email)
            .Take(2).ToListAsync();
        if (matches.Count != 1) return NotFound(new { message = "No unique student record is linked to this login. Contact your school administrator." });
        var student = matches[0];
        var enrollment = await _db.StudentEnrollment.AsNoTracking()
            .Where(x => x.StudentId == student.Id && x.SchoolId == student.SchoolId && x.IsActive && x.EnrollmentStatus == "Active")
            .OrderByDescending(x => x.EnrollmentDate).FirstOrDefaultAsync();
        if (enrollment == null) return NotFound(new { message = "No active enrollment was found for this student." });

        var school = await _db.Schools.AsNoTracking().Where(x => x.Id == student.SchoolId).Select(x => x.SchoolName).FirstOrDefaultAsync();
        var className = await _db.Classes.AsNoTracking().Where(x => x.Id == enrollment.ClassId).Select(x => x.ClassName).FirstOrDefaultAsync();
        var sectionName = await _db.SectionDetails.AsNoTracking().Where(x => x.Id == enrollment.SectionId).Select(x => x.SectionName).FirstOrDefaultAsync();
        var subjects = await (from link in _db.SectionSubjects.AsNoTracking()
            join subject in _db.Subjects on link.SubjectId equals subject.Id
            where link.SectionId == enrollment.SectionId && link.SchoolId == student.SchoolId && link.IsActive && subject.IsActive
            orderby subject.SubjectName
            select new { subject.Id, subject.SubjectName }).ToListAsync();
        var timetable = await (from slot in _db.Timetables.AsNoTracking()
            join period in _db.TimetablePeriods on slot.PeriodId equals period.Id
            join subject in _db.Subjects on slot.SubjectId equals subject.Id into subjectJoin
            from subject in subjectJoin.DefaultIfEmpty()
            where slot.SectionId == enrollment.SectionId && slot.SchoolId == student.SchoolId && slot.IsActive
            orderby slot.DayOfWeek, period.PeriodNumber
            select new { slot.DayOfWeek, period.PeriodNumber, period.StartTime, period.EndTime, period.IsBreak,
                SubjectName = subject == null ? "Break" : subject.SubjectName }).ToListAsync();
        var homework = await (from item in _db.HomeworkAssignments.AsNoTracking()
            join subject in _db.Subjects on item.SubjectId equals subject.Id
            where item.SectionId == enrollment.SectionId && item.SchoolId == student.SchoolId && item.IsActive && item.Status == "Published"
            orderby item.DueDate
            select new { item.Id, item.Title, item.Description, item.AssignedDate, item.DueDate,
                item.TotalMarks, item.ResourceUrl, subject.SubjectName }).ToListAsync();
        var materials = await (from item in _db.TeacherStudyMaterials.AsNoTracking()
            join subject in _db.Subjects on item.SubjectId equals subject.Id
            where item.SectionId == enrollment.SectionId && item.SchoolId == student.SchoolId && item.IsActive
            orderby item.CreatedDate descending
            select new { item.Id, item.Title, item.Description, item.ResourceType, item.ResourceUrl,
                item.CreatedDate, subject.SubjectName }).ToListAsync();
        var attendance = await _db.StudentAttendance.AsNoTracking()
            .Where(x => x.Student_Id == student.Id && x.EnrollmentId == enrollment.Id && x.School_Id == student.SchoolId && x.IsActive)
            .OrderByDescending(x => x.Attendance_Date)
            .Select(x => new { date = x.Attendance_Date, x.Status }).ToListAsync();
        var exams = await (from schedule in _db.ExamSchedules.AsNoTracking()
            join exam in _db.Exams on schedule.ExamId equals exam.Id
            join subject in _db.Subjects on schedule.SubjectId equals subject.Id into subjectJoin
            from subject in subjectJoin.DefaultIfEmpty()
            where schedule.SectionId == enrollment.SectionId && schedule.ClassId == enrollment.ClassId &&
                schedule.SchoolId == student.SchoolId && schedule.IsActive && exam.IsActive && exam.IsPublished &&
                exam.AcademicSessionId == enrollment.SessionId
            orderby schedule.ExamDate
            select new { schedule.Id, examName = exam.Name, schedule.ExamDate, schedule.StartTime,
                schedule.EndTime, subjectName = subject == null ? "" : subject.SubjectName }).ToListAsync();
        var results = await (from result in _db.ExamResults.AsNoTracking()
            join exam in _db.Exams on result.ExamId equals exam.Id
            where result.StudentId == student.Id && result.EnrollmentId == enrollment.Id && result.SchoolId == student.SchoolId &&
                result.Published && exam.ResultPublished && exam.IsActive
            select new { examName = exam.Name, result.TotalMarks, result.ObtainedMarks, result.Percentage,
                result.Grade, result.ResultStatus }).ToListAsync();
        var parent = await _db.ParentDetails.AsNoTracking().Where(x => x.Id == student.ParentId && x.IsActive)
            .Select(x => new { x.Name, x.Relationship, x.Email, x.PhoneNumber }).FirstOrDefaultAsync();
        var teachers = await (from mapping in _db.SectionSubjectTeachers.AsNoTracking()
            join staff in _db.Staff on mapping.StaffId equals staff.Id
            join subject in _db.Subjects on mapping.SubjectId equals subject.Id
            where mapping.SectionId == enrollment.SectionId && mapping.SchoolId == student.SchoolId &&
                mapping.IsActive && staff.IsActive && subject.IsActive
            orderby subject.SubjectName
            select new { staff.Id, staff.Name, subject.SubjectName }).ToListAsync();
        var documents = await _db.Student_Documents.AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new { x.Id, x.DocumentName, x.FileName, x.FileUrl, x.CreatedDate }).ToListAsync();
        var fees = await (from fee in _db.StudentFees.AsNoTracking()
            join type in _db.FeeTypes on fee.FeeTypeId equals type.Id
            where fee.StudentId == student.Id && fee.EnrollmentId == enrollment.Id && fee.SchoolId == student.SchoolId && fee.IsActive
            orderby type.Name
            select new { fee.Id, fee.Amount, fee.Status, feeType = type.Name }).ToListAsync();
        var feeIds = fees.Select(x => x.Id).ToList();
        var payments = await _db.FeePayments.AsNoTracking()
            .Where(x => feeIds.Contains(x.StudentFeeId) && x.SchoolId == student.SchoolId && x.IsActive)
            .OrderByDescending(x => x.Payment_Date)
            .Select(x => new { x.Id, x.StudentFeeId, x.AmountPaid, x.Payment_Date, x.Payment_Mode, x.Receipt_Number }).ToListAsync();
        var transport = await (from allocation in _db.StudentTransportAllocations.AsNoTracking()
            join assignment in _db.TransportVehicleAssignments on allocation.VehicleAssignmentId equals assignment.Id
            join route in _db.TransportRoutes on assignment.RouteId equals route.Id
            join vehicle in _db.TransportVehicles on assignment.VehicleId equals vehicle.Id
            where allocation.StudentId == student.Id && allocation.AcademicSessionId == enrollment.SessionId &&
                allocation.SchoolId == student.SchoolId && allocation.IsActive && assignment.IsActive
            select new { route.RouteName, vehicle.VehicleNumber, allocation.PickupStop,
                allocation.DropStop, allocation.SeatNumber, allocation.PickupShift, allocation.DropShift }).FirstOrDefaultAsync();
        var diary = await (from entry in _db.ClassDiaryEntries.AsNoTracking()
            join subject in _db.Subjects on entry.SubjectId equals subject.Id
            join staff in _db.Staff on entry.StaffId equals staff.Id
            where entry.SchoolId == student.SchoolId && entry.SectionId == enrollment.SectionId &&
                entry.IsActive && entry.IsPublished
            orderby entry.EntryDate descending
            select new { entry.Id, entry.EntryDate, entry.Topic, entry.Pages, entry.Homework,
                subject.SubjectName, teacherName = staff.Name }).Take(100).ToListAsync();
        var submissions = await _db.AssignmentSubmissions.AsNoTracking()
            .Where(x => x.StudentId == student.Id && x.EnrollmentId == enrollment.Id &&
                x.SchoolId == student.SchoolId && x.IsActive)
            .Select(x => new { x.Id, x.AssignmentId, x.SubmittedAt, x.TextAnswer, x.Status,
                x.TeacherFeedback, x.Marks, hasFile = x.FileUrl != null }).ToListAsync();
        var announcements = await _db.SchoolAnnouncements.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.IsActive && x.IsPublished &&
                (x.SectionId == null || x.SectionId == enrollment.SectionId) &&
                (x.ExpiresAt == null || x.ExpiresAt >= DateTime.UtcNow))
            .OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Title, x.Body, x.CreatedAt, x.IsPinned }).Take(50).ToListAsync();
        var libraryBooks = await _db.InventoryBooks.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.AcademicSessionId == enrollment.SessionId &&
                (x.ClassId == null || x.ClassId == enrollment.ClassId) &&
                (x.SectionId == null || x.SectionId == enrollment.SectionId))
            .OrderBy(x => x.BookName)
            .Select(x => new { x.Id, x.BookName, x.Publisher, x.Edition, x.Isbn, x.SubjectId })
            .Take(200).ToListAsync();
        var borrowedBooks = await (from order in _db.InventoryStudentOrders.AsNoTracking()
            join item in _db.InventoryStudentOrderItems on order.Id equals item.StudentOrderId
            join product in _db.InventoryProducts on item.ProductId equals product.Id
            where order.StudentId == student.Id && order.EnrollmentId == enrollment.Id &&
                order.SchoolId == student.SchoolId && order.OrderType == "Borrow"
            select new { title = product.ProductName, order.OrderNumber, order.BorrowDateTime,
                order.ReturnDateTime, order.Status }).ToListAsync();

        var requests = await _db.StudentServiceRequests.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Type, x.Subject, x.Details, x.FromDate, x.ToDate,
                x.Status, x.Response, x.CreatedAt, x.RespondedAt }).ToListAsync();
        var messages = await (from message in _db.TeacherStudentMessages.AsNoTracking()
            join staff in _db.Staff on message.StaffId equals staff.Id
            where message.SchoolId == student.SchoolId && message.StudentId == student.Id && message.IsActive
            orderby message.SentAt
            select new { message.Id, message.StaffId, teacherName = staff.Name, message.Body,
                message.FromStudent, message.SentAt }).ToListAsync();
        var achievements = await _db.StudentAchievements.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.StudentId == student.Id && x.IsActive)
            .OrderByDescending(x => x.AwardedAt)
            .Select(x => new { x.Id, x.Title, x.Description, x.AwardedAt }).ToListAsync();
        var schoolEvents = await _db.SchoolCalendarEvents.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.IsActive &&
                (x.SectionId == null || x.SectionId == enrollment.SectionId))
            .OrderBy(x => x.EventDate)
            .Select(x => new { x.Id, x.Title, x.Description, x.EventDate, x.EndDate, x.EventType }).ToListAsync();

        var markRows = await (from mark in _db.ExamMarks.AsNoTracking()
            join schedule in _db.ExamSchedules on mark.ExamScheduleId equals schedule.Id
            join exam in _db.Exams on mark.ExamId equals exam.Id
            join subject in _db.Subjects on schedule.SubjectId equals subject.Id
            where mark.StudentId == student.Id && mark.EnrollmentId == enrollment.Id &&
                mark.SchoolId == student.SchoolId && mark.IsActive && exam.ResultPublished && exam.IsActive
            orderby mark.EnteredDate descending
            select new { examId = exam.Id, examName = exam.Name, subjectId = subject.Id,
                subjectName = subject.SubjectName, mark.ObtainedMarks, mark.Remarks, mark.EnteredDate }).ToListAsync();
        var examIds = markRows.Select(x => x.examId).Distinct().ToList();
        var maxMarks = await _db.ExamSubjects.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.ClassId == enrollment.ClassId &&
                (x.SectionId == null || x.SectionId == enrollment.SectionId) &&
                examIds.Contains(x.ExamId) && x.IsActive)
            .Select(x => new { x.ExamId, x.SubjectId, x.MaxMarks }).ToListAsync();
        var gradeHistory = markRows.Select(row => new { row.examName, row.subjectName, row.ObtainedMarks,
            maxMarks = maxMarks.FirstOrDefault(x => x.ExamId == row.examId && x.SubjectId == row.subjectId)?.MaxMarks,
            row.Remarks, row.EnteredDate }).ToList();

        var examResources = await (from resource in _db.ExamLearningResources.AsNoTracking()
            join exam in _db.Exams on resource.ExamId equals exam.Id
            join subject in _db.Subjects on resource.SubjectId equals subject.Id
            where resource.SchoolId == student.SchoolId && resource.SectionId == enrollment.SectionId &&
                resource.IsActive && resource.IsPublished && exam.IsActive && exam.IsPublished &&
                exam.AcademicSessionId == enrollment.SessionId
            select new { resource.Id, resource.ExamId, examName = exam.Name, subjectName = subject.SubjectName,
                resource.Syllabus, resource.ResourceUrl }).ToListAsync();
        var hallTickets = await (from ticket in _db.StudentHallTickets.AsNoTracking()
            join exam in _db.Exams on ticket.ExamId equals exam.Id
            where ticket.SchoolId == student.SchoolId && ticket.StudentId == student.Id &&
                ticket.IsActive && ticket.IsPublished && exam.IsActive && exam.IsPublished
            select new { ticket.Id, ticket.ExamId, examName = exam.Name, ticket.SeatNumber, ticket.Room,
                ticket.DocumentUrl }).ToListAsync();
        var onlineExamIds = await _db.OnlineExamQuestions.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive)
            .Select(x => x.ExamId).Distinct().ToListAsync();
        var onlineExams = await _db.Exams.AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId && x.AcademicSessionId == enrollment.SessionId &&
                x.IsActive && x.IsPublished && onlineExamIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate }).ToListAsync();
        var onlineAttempts = await _db.OnlineExamAttempts.AsNoTracking()
            .Where(x => x.StudentId == student.Id && x.EnrollmentId == enrollment.Id &&
                x.SchoolId == student.SchoolId)
            .Select(x => new { x.Id, x.ExamId, x.CorrectCount, x.TotalQuestions, x.SubmittedAt }).ToListAsync();

        return Ok(new { success = true, data = new {
            profile = new { student.StudentName, student.Email, rollNumber = enrollment.RollNumber ?? student.Rollnumber,
                schoolName = school, className, sectionName },
            subjects, timetable, homework, materials, attendance, exams, results, parent, teachers, documents, fees, payments, transport, diary, submissions, announcements, libraryBooks, borrowedBooks, requests, messages, achievements, schoolEvents, gradeHistory, examResources, hallTickets, onlineExams, onlineAttempts
        } });
    }
    private async Task<(SchoolManagement.Model.Students? student, SchoolManagement.Model.StudentEnrollment? enrollment)> CurrentEnrollment()
    {
        if (!User.IsInRole("Student") || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId)) return (null, null);
        var credential = await _db.Students_Parents_Creds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == credentialId && x.IsActive && x.RoleName == "Student" && x.Status == "Active");
        if (credential == null) return (null, null);
        var matches = await _db.Students.AsNoTracking().Where(x => x.SchoolId == credential.School_Id && x.Email == credential.Email && x.IsActive).Take(2).ToListAsync();
        if (matches.Count != 1) return (null, null);
        var student = matches[0];
        var enrollment = await _db.StudentEnrollment.AsNoTracking().Where(x => x.StudentId == student.Id && x.SchoolId == student.SchoolId && x.IsActive && x.EnrollmentStatus == "Active").OrderByDescending(x => x.EnrollmentDate).FirstOrDefaultAsync();
        return (student, enrollment);
    }
    [HttpPost("submissions")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] StudentSubmissionInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null) return Forbid();
        var assignment = await _db.HomeworkAssignments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.AssignmentId && x.SchoolId == student.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive && x.Status == "Published");
        if (assignment == null) return NotFound(new { message = "Assignment is not available for your class." });
        var answer = input.TextAnswer?.Trim();
        if (string.IsNullOrWhiteSpace(answer) && input.File == null) return BadRequest(new { message = "Add an answer or upload a file." });
        var existing = await _db.AssignmentSubmissions.FirstOrDefaultAsync(x => x.AssignmentId == assignment.Id && x.StudentId == student.Id && x.IsActive);
        if (existing != null && existing.Status != "Resubmission Required") return Conflict(new { message = "You already submitted this assignment." });
        string? storedFile = null;
        if (input.File != null)
        {
            var allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
                [".pdf"] = new[] { "application/pdf" }, [".doc"] = new[] { "application/msword" },
                [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                [".jpg"] = new[] { "image/jpeg" }, [".jpeg"] = new[] { "image/jpeg" },
                [".png"] = new[] { "image/png" }, [".webp"] = new[] { "image/webp" }
            };
            var extension = Path.GetExtension(input.File.FileName).ToLowerInvariant();
            if (input.File.Length == 0 || input.File.Length > 10 * 1024 * 1024 || !allowed.TryGetValue(extension, out var types) || !types.Contains(input.File.ContentType))
                return BadRequest(new { message = "Upload a PDF, Word document or image up to 10 MB." });
            var folder = Path.Combine(_env.ContentRootPath, "private-uploads", "student-submissions");
            Directory.CreateDirectory(folder);
            storedFile = Guid.NewGuid().ToString("N") + extension;
            await using var stream = new FileStream(Path.Combine(folder, storedFile), FileMode.CreateNew);
            await input.File.CopyToAsync(stream);
        }
        var status = assignment.DueDate < DateTime.Now ? "Late" : "Submitted";
        if (existing == null)
            _db.AssignmentSubmissions.Add(new SchoolManagement.Model.AssignmentSubmission {
                SchoolId = student.SchoolId, AssignmentId = assignment.Id, StudentId = student.Id, EnrollmentId = enrollment.Id,
                TextAnswer = answer, FileUrl = storedFile, SubmittedAt = DateTime.UtcNow, Status = status
            });
        else
        {
            existing.TextAnswer = answer; existing.FileUrl = storedFile ?? existing.FileUrl;
            existing.SubmittedAt = DateTime.UtcNow; existing.Status = status;
            existing.Marks = null; existing.TeacherFeedback = null;
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Assignment submitted." });
    }
    [HttpGet("submissions/{id:int}/file")]
    public async Task<IActionResult> DownloadSubmissionFile(int id)
    {
        var (student, _) = await CurrentEnrollment();
        if (student == null) return Forbid();
        var submission = await _db.AssignmentSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.StudentId == student.Id && x.SchoolId == student.SchoolId && x.IsActive);
        if (submission?.FileUrl == null) return NotFound();
        var path = Path.Combine(_env.ContentRootPath, "private-uploads", "student-submissions", Path.GetFileName(submission.FileUrl));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/octet-stream", "submission" + Path.GetExtension(path));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] StudentRequestInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null) return Forbid();
        var allowed = new[] { "Leave", "Certificate", "ID Card", "General", "Helpdesk", "Transport", "Lost & Found" };
        if (!allowed.Contains(input.Type) || string.IsNullOrWhiteSpace(input.Subject) ||
            string.IsNullOrWhiteSpace(input.Details) || input.Subject.Length > 200 || input.Details.Length > 2000)
            return BadRequest(new { message = "Choose a request type and enter a subject and details." });
        if (input.Type == "Leave" && (!input.FromDate.HasValue || !input.ToDate.HasValue ||
            input.FromDate.Value.Date < DateTime.Today || input.ToDate.Value.Date < input.FromDate.Value.Date))
            return BadRequest(new { message = "Choose a valid future leave date range." });
        var request = new SchoolManagement.Model.StudentServiceRequest {
            SchoolId = student.SchoolId, StudentId = student.Id, EnrollmentId = enrollment.Id,
            Type = input.Type, Subject = input.Subject.Trim(), Details = input.Details.Trim(),
            FromDate = input.FromDate?.Date, ToDate = input.ToDate?.Date
        };
        _db.StudentServiceRequests.Add(request);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { request.Id } });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] StudentMessageInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000)
            return BadRequest(new { message = "Enter a message up to 2000 characters." });
        if (!await _db.SectionSubjectTeachers.AnyAsync(x => x.SchoolId == student.SchoolId &&
            x.SectionId == enrollment.SectionId && x.StaffId == input.StaffId && x.IsActive))
            return Forbid();
        var message = new SchoolManagement.Model.TeacherStudentMessage {
            SchoolId = student.SchoolId, StudentId = student.Id, StaffId = input.StaffId,
            Body = input.Body.Trim(), FromStudent = true
        };
        _db.TeacherStudentMessages.Add(message);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { message.Id } });
    }

    [HttpGet("online-exams/{examId:int}")]
    public async Task<IActionResult> OnlineExam(int examId)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null) return Forbid();
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId &&
            x.SchoolId == student.SchoolId && x.AcademicSessionId == enrollment.SessionId &&
            x.IsActive && x.IsPublished);
        if (exam == null) return NotFound();
        if (!exam.StartDate.HasValue || !exam.EndDate.HasValue ||
            DateTime.Today < exam.StartDate.Value.Date || DateTime.Today > exam.EndDate.Value.Date)
            return BadRequest(new { message = "This online exam is not open today." });
        if (await _db.OnlineExamAttempts.AnyAsync(x => x.ExamId == examId && x.StudentId == student.Id))
            return Conflict(new { message = "You already submitted this online exam." });
        var questions = await _db.OnlineExamQuestions.AsNoTracking()
            .Where(x => x.ExamId == examId && x.SchoolId == student.SchoolId &&
                x.SectionId == enrollment.SectionId && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Question, x.OptionA, x.OptionB, x.OptionC, x.OptionD }).ToListAsync();
        if (questions.Count == 0) return NotFound(new { message = "No questions are published for your class." });
        return Ok(new { success = true, data = new { exam.Id, exam.Name, questions } });
    }

    [HttpPost("online-exams/{examId:int}/submit")]
    public async Task<IActionResult> SubmitOnlineExam(int examId, [FromBody] OnlineExamAnswerInput input)
    {
        var (student, enrollment) = await CurrentEnrollment();
        if (student == null || enrollment == null) return Forbid();
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId &&
            x.SchoolId == student.SchoolId && x.AcademicSessionId == enrollment.SessionId &&
            x.IsActive && x.IsPublished);
        if (exam == null) return NotFound();
        if (!exam.StartDate.HasValue || !exam.EndDate.HasValue ||
            DateTime.Today < exam.StartDate.Value.Date || DateTime.Today > exam.EndDate.Value.Date)
            return BadRequest(new { message = "This online exam is not open today." });
        if (await _db.OnlineExamAttempts.AnyAsync(x => x.ExamId == examId && x.StudentId == student.Id))
            return Conflict(new { message = "You already submitted this online exam." });
        var questions = await _db.OnlineExamQuestions.AsNoTracking()
            .Where(x => x.ExamId == examId && x.SchoolId == student.SchoolId &&
                x.SectionId == enrollment.SectionId && x.IsActive).ToListAsync();
        if (questions.Count == 0) return NotFound();
        var answers = input.Answers ?? new Dictionary<int, string>();
        var correct = questions.Count(x => answers.TryGetValue(x.Id, out var choice) &&
            string.Equals(choice, x.CorrectOption, StringComparison.OrdinalIgnoreCase));
        var attempt = new SchoolManagement.Model.OnlineExamAttempt {
            SchoolId = student.SchoolId, StudentId = student.Id, EnrollmentId = enrollment.Id,
            ExamId = examId, AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            CorrectCount = correct, TotalQuestions = questions.Count
        };
        _db.OnlineExamAttempts.Add(attempt);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { message = "This exam was already submitted." }); }
        return Ok(new { success = true, data = new { attempt.Id, attempt.CorrectCount, attempt.TotalQuestions } });
    }

}




public class StudentSubmissionInput { public int AssignmentId { get; set; } public string? TextAnswer { get; set; } public IFormFile? File { get; set; } }



public class StudentRequestInput { public string Type { get; set; } = ""; public string Subject { get; set; } = ""; public string Details { get; set; } = ""; public DateTime? FromDate { get; set; } public DateTime? ToDate { get; set; } }
public class StudentMessageInput { public int StaffId { get; set; } public string Body { get; set; } = ""; }







public class OnlineExamAnswerInput { public Dictionary<int, string> Answers { get; set; } = new(); }

