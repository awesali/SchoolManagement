namespace SchoolManagement.DTOs
{
    public class CreateExamScheduleRequest
    {
        // 🔥 EXAM (GROUP LEVEL)
        public string Name { get; set; }          // e.g. "Nursery Midterm April"
        public int ExamTypeId { get; set; }
        public int SchoolId { get; set; }
        public DateTime StartDate { get; set; }   // overall exam start
        public DateTime EndDate { get; set; }     // overall exam end

        // 🔽 DETAILS
        public List<ClassScheduleDto> Classes { get; set; }
    }

    public class ClassScheduleDto
    {
        public int ClassId { get; set; }
        public List<SectionScheduleDto> Sections { get; set; }
    }

    public class SectionScheduleDto
    {
        public int SectionId { get; set; }
        public List<SubjectScheduleDto> Subjects { get; set; }
    }

    public class SubjectScheduleDto
    {
        public int SubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}