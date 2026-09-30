// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the save marks request and response data.
    public class SaveMarksDto
    {
        public int SchoolId { get; set; }

        public int ExamId { get; set; }

        public int ExamScheduleId { get; set; }

        public int SectionId { get; set; }

        public int SubjectId { get; set; }

        public List<StudentMarksDto> Marks { get; set; }
    }
}
