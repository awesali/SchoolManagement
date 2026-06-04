namespace SchoolManagement.DTOs
{
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
