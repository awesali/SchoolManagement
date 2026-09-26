using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class ExamLearningResource
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int ExamId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    [MaxLength(4000)] public string Syllabus { get; set; } = "";
    [MaxLength(1000)] public string? ResourceUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
public class StudentHallTicket
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int ExamId { get; set; }
    [MaxLength(50)] public string SeatNumber { get; set; } = "";
    [MaxLength(100)] public string Room { get; set; } = "";
    [MaxLength(200)] public string? Venue { get; set; }
    [MaxLength(1000)] public string? DocumentUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
}
