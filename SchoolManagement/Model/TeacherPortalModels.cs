using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model;

public class HomeworkAssignment
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(3000)] public string Description { get; set; } = "";
    public DateTime AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? TotalMarks { get; set; }
    [MaxLength(1000)] public string? ResourceUrl { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Published";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class TeacherStudyMaterial
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(30)] public string ResourceType { get; set; } = "Link";
    [MaxLength(1000)] public string ResourceUrl { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class StaffLeaveRequest
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }
    [MaxLength(50)] public string LeaveType { get; set; } = "Casual Leave";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(1000)] public string? AdminRemarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
