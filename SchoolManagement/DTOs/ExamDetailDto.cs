namespace SchoolManagement.DTOs
{
    public class ExamDetailDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public string ExamType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ExamDetailClassDto> Classes { get; set; }
    }

    public class ExamDetailClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<ExamDetailSectionDto> Sections { get; set; }
    }

    public class ExamDetailSectionDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public List<ExamDetailSubjectDto> Subjects { get; set; }
    }

    public class ExamDetailSubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
