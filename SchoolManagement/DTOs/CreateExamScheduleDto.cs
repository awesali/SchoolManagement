namespace SchoolManagement.DTOs
{
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
