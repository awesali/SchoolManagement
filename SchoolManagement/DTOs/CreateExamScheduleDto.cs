namespace SchoolManagement.DTOs
{
    public class SubjectScheduleItemDto
    {
        public int SubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class CreateExamScheduleDto
    {
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<SubjectScheduleItemDto> Subjects { get; set; } = new();
    }
}
