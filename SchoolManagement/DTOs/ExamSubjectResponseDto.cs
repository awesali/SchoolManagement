namespace SchoolManagement.DTOs
{
    public class ExamSubjectResponseDto
    {
        public int Id { get; set; }

        public int ClassId { get; set; }
        public string ClassName { get; set; }

        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        public decimal MaxMarks { get; set; }
        public DateTime? ExamDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public decimal PassingMarks { get; set; }
    }
}
