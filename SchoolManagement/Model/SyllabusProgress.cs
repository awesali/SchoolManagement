using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

// Append-only snapshots retain who recorded each update and when.
public class SyllabusProgress
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int SessionId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public int StaffId { get; set; }
    public DateTime ProgressDate { get; set; }
    public int TotalChapters { get; set; }
    public int PlannedChapters { get; set; }
    public int CompletedChapters { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
