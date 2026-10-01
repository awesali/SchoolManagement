using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Service;

public interface IEventEmailService
{
    Task SendToStudentAsync(int schoolId, int studentId, string templateName,
        Dictionary<string, string> values, EmailAttachment? attachment = null);
    Task SendToSectionAsync(int schoolId, int? sessionId, int? classId, int? sectionId,
        string templateName, Dictionary<string, string> values,
        Func<int, EmailAttachment?>? attachmentForStudent = null);
}

public sealed class EventEmailService : IEventEmailService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    public EventEmailService(AppDbContext db, IEmailService email) { _db = db; _email = email; }

    private sealed record Recipient(int StudentId, string StudentName, string? StudentEmail,
        string? ParentEmail, string? ClassName, string? SectionName, string? RollNumber);

    public async Task SendToStudentAsync(int schoolId, int studentId, string templateName,
        Dictionary<string, string> values, EmailAttachment? attachment = null)
    {
        var recipients = await FindRecipients(schoolId, null, null, null, studentId);
        var student = recipients.FirstOrDefault();
        if (student == null) throw new InvalidOperationException("Student email recipient was not found.");
        await SendOne(schoolId, student, templateName, values, attachment);
    }

    public async Task SendToSectionAsync(int schoolId, int? sessionId, int? classId, int? sectionId,
        string templateName, Dictionary<string, string> values,
        Func<int, EmailAttachment?>? attachmentForStudent = null)
    {
        var recipients = await FindRecipients(schoolId, sessionId, classId, sectionId, null);
        foreach (var student in recipients)
            await SendOne(schoolId, student, templateName, values, attachmentForStudent?.Invoke(student.StudentId));
    }

    private async Task<List<Recipient>> FindRecipients(int schoolId, int? sessionId, int? classId,
        int? sectionId, int? studentId)
    {
        var rows = await (from enrollment in _db.StudentEnrollment.AsNoTracking()
            join student in _db.Students.AsNoTracking() on enrollment.StudentId equals student.Id
            join parent in _db.ParentDetails.AsNoTracking() on student.ParentId equals parent.Id into parents
            from parent in parents.DefaultIfEmpty()
            join schoolClass in _db.Classes.AsNoTracking() on enrollment.ClassId equals schoolClass.Id
            join section in _db.SectionDetails.AsNoTracking() on enrollment.SectionId equals section.Id
            where enrollment.SchoolId == schoolId && enrollment.IsActive && enrollment.EnrollmentStatus == "Active"
                && student.SchoolId == schoolId && student.IsActive
                && (!sessionId.HasValue || enrollment.SessionId == sessionId.Value)
                && (!classId.HasValue || enrollment.ClassId == classId.Value)
                && (!sectionId.HasValue || enrollment.SectionId == sectionId.Value)
                && (!studentId.HasValue || student.Id == studentId.Value)
            orderby enrollment.EnrollmentDate descending
            select new Recipient(student.Id, student.StudentName, student.Email,
                parent == null || !parent.IsActive ? null : parent.Email,
                schoolClass.ClassName, section.SectionName, enrollment.RollNumber)).ToListAsync();
        return rows.DistinctBy(row => row.StudentId).ToList();
    }

    private async Task SendOne(int schoolId, Recipient recipient, string templateName,
        Dictionary<string, string> values, EmailAttachment? attachment)
    {
        if (string.IsNullOrWhiteSpace(recipient.StudentEmail))
            throw new InvalidOperationException($"Student {recipient.StudentId} has no email address.");
        var schoolName = await _db.Schools.AsNoTracking().Where(s => s.Id == schoolId)
            .Select(s => s.SchoolName).FirstOrDefaultAsync() ?? "Your school";
        var placeholders = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
        {
            ["SchoolName"] = schoolName,
            ["StudentName"] = recipient.StudentName,
            ["ClassName"] = recipient.ClassName ?? "",
            ["SectionName"] = recipient.SectionName ?? "",
            ["RollNumber"] = recipient.RollNumber ?? "",
            ["LoginUrl"] = _email.LoginUrl,
        };
        var (subject, body) = await _email.GetEmailTemplateAsync(templateName, placeholders);
        var addresses = new[] { recipient.StudentEmail, recipient.ParentEmail }
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        await _email.SendEmailAsync(addresses, subject, body,
            attachment == null ? Array.Empty<EmailAttachment>() : new[] { attachment });
    }
}