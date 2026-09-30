// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the exam schedule list request and response data.
    public class ExamScheduleListDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public string ExamTitle { get; set; } // ExamType
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int ClassCount { get; set; }
    }
}
