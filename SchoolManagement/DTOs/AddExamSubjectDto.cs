// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the add exam subject request and response data.
    public class AddExamSubjectDto
    {
        public int SchoolId { get; set; }

        public int ExamId { get; set; }

        public int ClassId { get; set; }

        public int? SectionId { get; set; }

        public int SubjectId { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal PassingMarks { get; set; }
    }
}
