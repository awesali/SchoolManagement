using System.ComponentModel.DataAnnotations;
namespace SchoolManagement.Model;
public class StaffLeaveAllocation
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int StaffId { get; set; }
    public int AcademicSessionId { get; set; }
    [MaxLength(50)] public string LeaveType { get; set; } = "";
    public int Days { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
}
