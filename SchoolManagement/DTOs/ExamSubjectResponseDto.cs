namespace SchoolManagement.DTOs
{
    public class ExamSubjectResponseDto
    {
        public int Id { get; set; }

        public string SubjectName { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal PassingMarks { get; set; }
    }
}
