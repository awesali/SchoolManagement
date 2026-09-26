using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class StudentServiceRequest
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int EnrollmentId { get; set; }
    [MaxLength(40)] public string Type { get; set; } = "General";
    [MaxLength(200)] public string Subject { get; set; } = "";
    [MaxLength(2000)] public string Details { get; set; } = "";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(2000)] public string? Response { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
public class TeacherStudentMessage
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int StaffId { get; set; }
    [MaxLength(2000)] public string Body { get; set; } = "";
    public bool FromStudent { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
public class StudentAchievement
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public DateTime AwardedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
public class SchoolCalendarEvent
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int? SectionId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(30)] public string EventType { get; set; } = "Event";
    public bool IsActive { get; set; } = true;
}

