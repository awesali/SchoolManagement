// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the assign invigilator request and response data.
    public class AssignInvigilatorDto
    {
        public int ExamScheduleId { get; set; }
        public int StaffId { get; set; }
        public string DutyType { get; set; } // Main / Assistant
    }
}
