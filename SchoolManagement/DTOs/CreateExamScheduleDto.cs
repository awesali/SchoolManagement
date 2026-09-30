// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the create exam schedule request and response data.
    public class CreateExamScheduleDto
    {
        public int ExamId { get; set; }

        public int SchoolId { get; set; }

        public int ClassId { get; set; }

        public int SectionId { get; set; }

        public int SubjectId { get; set; }

        public DateTime ExamDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }
    }
}
