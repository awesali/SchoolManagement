using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/Teacher")]
public class TeacherStudentContentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public TeacherStudentContentController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    private async Task<Staff?> CurrentStaff()
    {
        if (User.FindFirstValue("RoleId") != "2" ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == userId && x.IsActive);
    }
    private Task<bool> CanTeach(Staff staff, int sectionId, int subjectId) =>
        _db.SectionSubjectTeachers.AnyAsync(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId &&
            x.SectionId == sectionId && x.SubjectId == subjectId && x.IsActive);

    [HttpGet("diary")]
    public async Task<IActionResult> Diary()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var entries = await (from entry in _db.ClassDiaryEntries.AsNoTracking()
            join section in _db.SectionDetails on entry.SectionId equals section.Id
            join subject in _db.Subjects on entry.SubjectId equals subject.Id
            where entry.StaffId == staff.Id && entry.SchoolId == staff.SchoolId && entry.IsActive
            orderby entry.EntryDate descending
            select new { entry.Id, entry.EntryDate, entry.Topic, entry.Pages, entry.Homework,
                entry.IsPublished, entry.SectionId, entry.SubjectId, section.SectionName, subject.SubjectName }).Take(150).ToListAsync();
        return Ok(new { success = true, data = entries });
    }

    [HttpPost("diary")]
    public async Task<IActionResult> CreateDiary([FromBody] DiaryInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await CanTeach(staff, input.SectionId, input.SubjectId)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Topic) || input.Topic.Length > 500 || input.EntryDate.Date > DateTime.Today)
            return BadRequest(new { message = "Enter a topic and a valid class date." });
        var entry = new ClassDiaryEntry { SchoolId = staff.SchoolId, StaffId = staff.Id, SectionId = input.SectionId,
            SubjectId = input.SubjectId, EntryDate = input.EntryDate.Date, Topic = input.Topic.Trim(),
            Pages = input.Pages?.Trim(), Homework = input.Homework?.Trim(), IsPublished = input.Publish };
        _db.ClassDiaryEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { entry.Id } });
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> Submissions([FromQuery] int assignmentId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var assignment = await _db.HomeworkAssignments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (assignment == null) return NotFound();
        var rows = await (from submission in _db.AssignmentSubmissions.AsNoTracking()
            join student in _db.Students on submission.StudentId equals student.Id
            where submission.AssignmentId == assignmentId && submission.SchoolId == staff.SchoolId && submission.IsActive
            orderby submission.SubmittedAt descending
            select new { submission.Id, student.StudentName, submission.SubmittedAt, submission.TextAnswer,
                submission.Status, submission.Marks, submission.TeacherFeedback, hasFile = submission.FileUrl != null }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("submissions/{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] SubmissionReviewInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var submission = await _db.AssignmentSubmissions.FirstOrDefaultAsync(x => x.Id == id &&
            x.SchoolId == staff.SchoolId && x.IsActive);
        if (submission == null) return NotFound();
        var assignment = await _db.HomeworkAssignments.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == submission.AssignmentId && x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (assignment == null) return Forbid();
        if (!new[] { "Graded", "Returned", "Resubmission Required" }.Contains(input.Status) ||
            input.Marks < 0 || (assignment.TotalMarks.HasValue && input.Marks > assignment.TotalMarks))
            return BadRequest(new { message = "Choose a valid status and marks within the assignment total." });
        submission.Status = input.Status;
        submission.Marks = input.Status == "Graded" ? input.Marks : null;
        submission.TeacherFeedback = input.Feedback?.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("submissions/{id:int}/file")]
    public async Task<IActionResult> DownloadSubmissionFile(int id)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var submission = await _db.AssignmentSubmissions.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == id && x.SchoolId == staff.SchoolId && x.IsActive);
        if (submission?.FileUrl == null) return NotFound();
        if (!await _db.HomeworkAssignments.AnyAsync(x => x.Id == submission.AssignmentId &&
            x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive)) return Forbid();
        var path = Path.Combine(_env.ContentRootPath, "private-uploads", "student-submissions", Path.GetFileName(submission.FileUrl));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/octet-stream", "submission" + Path.GetExtension(path));
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> Announcements()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await _db.SchoolAnnouncements.AsNoTracking()
            .Where(x => x.SchoolId == staff.SchoolId && x.CreatedBy == staff.Id && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.SectionId, x.Title, x.Body, x.CreatedAt, x.ExpiresAt,
                x.IsPinned, x.IsPublished }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!input.SectionId.HasValue || !await _db.SectionSubjectTeachers.AnyAsync(x => x.StaffId == staff.Id &&
            x.SchoolId == staff.SchoolId && x.SectionId == input.SectionId.Value && x.IsActive)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Body) ||
            input.Title.Length > 200 || input.Body.Length > 4000)
            return BadRequest(new { message = "Enter a title and message." });
        var item = new SchoolAnnouncement { SchoolId = staff.SchoolId, SectionId = input.SectionId,
            CreatedBy = staff.Id, Title = input.Title.Trim(), Body = input.Body.Trim(),
            ExpiresAt = input.ExpiresAt, IsPinned = input.IsPinned, IsPublished = input.Publish };
        _db.SchoolAnnouncements.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpGet("messages")]
    public async Task<IActionResult> Messages()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await (from message in _db.TeacherStudentMessages.AsNoTracking()
            join student in _db.Students on message.StudentId equals student.Id
            where message.SchoolId == staff.SchoolId && message.StaffId == staff.Id && message.IsActive
            orderby message.SentAt descending
            select new { message.Id, message.StudentId, student.StudentName, message.Body,
                message.FromStudent, message.SentAt }).Take(200).ToListAsync();
        return Ok(new { success = true, data = rows });
    }
    [HttpPost("messages")]
    public async Task<IActionResult> Reply([FromBody] TeacherMessageInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 2000)
            return BadRequest(new { message = "Enter a message up to 2000 characters." });
        var enrollment = await _db.StudentEnrollment.AsNoTracking()
            .Where(x => x.StudentId == input.StudentId && x.SchoolId == staff.SchoolId &&
                x.IsActive && x.EnrollmentStatus == "Active")
            .OrderByDescending(x => x.EnrollmentDate).FirstOrDefaultAsync();
        if (enrollment == null || !await _db.SectionSubjectTeachers.AnyAsync(x => x.StaffId == staff.Id &&
            x.SchoolId == staff.SchoolId && x.SectionId == enrollment.SectionId && x.IsActive)) return Forbid();
        var item = new TeacherStudentMessage { SchoolId = staff.SchoolId, StaffId = staff.Id,
            StudentId = input.StudentId, Body = input.Body.Trim(), FromStudent = false };
        _db.TeacherStudentMessages.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }

    [HttpGet("exam-options")]
    public async Task<IActionResult> ExamOptions()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await _db.Exams.AsNoTracking().Where(x => x.SchoolId == staff.SchoolId && x.IsActive)
            .OrderByDescending(x => x.StartDate).Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate,
                x.IsPublished }).Take(100).ToListAsync();
        return Ok(new { success = true, data = rows });
    }
    [HttpGet("exam-resources")]
    public async Task<IActionResult> ExamResources()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var mappings = await _db.SectionSubjectTeachers.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive)
            .Select(x => new { x.SectionId, x.SubjectId }).ToListAsync();
        var sectionIds = mappings.Select(x => x.SectionId).Distinct().ToList();
        var rows = await _db.ExamLearningResources.AsNoTracking()
            .Where(x => x.SchoolId == staff.SchoolId && sectionIds.Contains(x.SectionId) && x.IsActive)
            .Select(x => new { x.Id, x.ExamId, x.SectionId, x.SubjectId, x.Syllabus, x.ResourceUrl,
                x.IsPublished }).ToListAsync();
        return Ok(new { success = true, data = rows.Where(x => mappings.Any(m =>
            m.SectionId == x.SectionId && m.SubjectId == x.SubjectId)) });
    }
    [HttpPost("exam-resources")]
    public async Task<IActionResult> SaveExamResource([FromBody] TeacherExamResourceInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await CanTeach(staff, input.SectionId, input.SubjectId)) return Forbid();
        if (!await _db.Exams.AnyAsync(x => x.Id == input.ExamId && x.SchoolId == staff.SchoolId && x.IsActive))
            return BadRequest(new { message = "Choose an exam from your school." });
        if (string.IsNullOrWhiteSpace(input.Syllabus) || input.Syllabus.Length > 4000)
            return BadRequest(new { message = "Enter a syllabus up to 4000 characters." });
        if (!string.IsNullOrEmpty(input.ResourceUrl) &&
            (!Uri.TryCreate(input.ResourceUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return BadRequest(new { message = "Enter a valid syllabus link." });
        var item = await _db.ExamLearningResources.FirstOrDefaultAsync(x => x.SchoolId == staff.SchoolId &&
            x.ExamId == input.ExamId && x.SectionId == input.SectionId && x.SubjectId == input.SubjectId && x.IsActive);
        if (item == null)
        {
            item = new ExamLearningResource { SchoolId = staff.SchoolId, ExamId = input.ExamId,
                SectionId = input.SectionId, SubjectId = input.SubjectId };
            _db.ExamLearningResources.Add(item);
        }
        item.Syllabus = input.Syllabus.Trim(); item.ResourceUrl = input.ResourceUrl?.Trim();
        item.IsPublished = input.Publish;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpGet("online-questions")]
    public async Task<IActionResult> OnlineQuestions([FromQuery] int examId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var mappings = await _db.SectionSubjectTeachers.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive)
            .Select(x => new { x.SectionId, x.SubjectId }).ToListAsync();
        var sections = mappings.Select(x => x.SectionId).Distinct().ToList();
        var rows = await _db.OnlineExamQuestions.AsNoTracking()
            .Where(x => x.ExamId == examId && x.SchoolId == staff.SchoolId && sections.Contains(x.SectionId) && x.IsActive)
            .ToListAsync();
        return Ok(new { success = true, data = rows.Where(x => mappings.Any(m =>
            m.SectionId == x.SectionId && m.SubjectId == x.SubjectId)) });
    }
    [HttpPost("online-questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] TeacherOnlineQuestionInput input)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await CanTeach(staff, input.SectionId, input.SubjectId)) return Forbid();
        if (!await _db.Exams.AnyAsync(x => x.Id == input.ExamId && x.SchoolId == staff.SchoolId && x.IsActive))
            return BadRequest(new { message = "Choose a school exam." });
        if (new[] { input.Question, input.OptionA, input.OptionB, input.OptionC, input.OptionD }.Any(string.IsNullOrWhiteSpace) ||
            !new[] { "A", "B", "C", "D" }.Contains(input.CorrectOption?.ToUpperInvariant()))
            return BadRequest(new { message = "Enter a question, four options, and the correct choice." });
        var item = new OnlineExamQuestion { SchoolId = staff.SchoolId, ExamId = input.ExamId,
            SectionId = input.SectionId, SubjectId = input.SubjectId, Question = input.Question.Trim(),
            OptionA = input.OptionA.Trim(), OptionB = input.OptionB.Trim(), OptionC = input.OptionC.Trim(),
            OptionD = input.OptionD.Trim(), CorrectOption = input.CorrectOption.ToUpperInvariant() };
        _db.OnlineExamQuestions.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { item.Id } });
    }
    [HttpGet("online-attempts")]
    public async Task<IActionResult> OnlineAttempts([FromQuery] int examId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var sections = await _db.SectionSubjectTeachers.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive)
            .Select(x => x.SectionId).Distinct().ToListAsync();
        var rows = await (from attempt in _db.OnlineExamAttempts.AsNoTracking()
            join enrollment in _db.StudentEnrollment on attempt.EnrollmentId equals enrollment.Id
            join student in _db.Students on attempt.StudentId equals student.Id
            where attempt.ExamId == examId && attempt.SchoolId == staff.SchoolId &&
                sections.Contains(enrollment.SectionId)
            select new { student.StudentName, attempt.CorrectCount, attempt.TotalQuestions,
                attempt.SubmittedAt }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

}
public class DiaryInput
{
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public DateTime EntryDate { get; set; }
    public string Topic { get; set; } = "";
    public string? Pages { get; set; }
    public string? Homework { get; set; }
    public bool Publish { get; set; } = true;
}
public class SubmissionReviewInput
{
    public string Status { get; set; } = "";
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
}
public class AnnouncementInput
{
    public int? SectionId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public bool Publish { get; set; } = true;
}


public class TeacherMessageInput { public int StudentId { get; set; } public string Body { get; set; } = ""; }


public class TeacherExamResourceInput { public int ExamId { get; set; } public int SectionId { get; set; } public int SubjectId { get; set; } public string Syllabus { get; set; } = ""; public string? ResourceUrl { get; set; } public bool Publish { get; set; } = true; }
public class TeacherOnlineQuestionInput { public int ExamId { get; set; } public int SectionId { get; set; } public int SubjectId { get; set; } public string Question { get; set; } = ""; public string OptionA { get; set; } = ""; public string OptionB { get; set; } = ""; public string OptionC { get; set; } = ""; public string OptionD { get; set; } = ""; public string CorrectOption { get; set; } = ""; }

