using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class ClassDiaryEntry
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public int StaffId { get; set; }
    public DateTime EntryDate { get; set; }
    [MaxLength(500)] public string Topic { get; set; } = "";
    [MaxLength(100)] public string? Pages { get; set; }
    [MaxLength(2000)] public string? Homework { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
public class AssignmentSubmission
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public int EnrollmentId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(4000)] public string? TextAnswer { get; set; }
    [MaxLength(1000)] public string? FileUrl { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Submitted";
    [MaxLength(2000)] public string? TeacherFeedback { get; set; }
    public decimal? Marks { get; set; }
    public bool IsActive { get; set; } = true;
}
public class SchoolAnnouncement
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int? SectionId { get; set; }
    public int CreatedBy { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(4000)] public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

