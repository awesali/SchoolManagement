namespace SchoolManagement.DTOs
{
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
