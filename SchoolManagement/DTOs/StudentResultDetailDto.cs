namespace SchoolManagement.DTOs
{
    public class StudentResultDetailDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }

        public string ExamName { get; set; }

        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }

        public decimal Percentage { get; set; }

        public string Grade { get; set; }

        public string ResultStatus { get; set; }

        public List<StudentSubjectResultDto> Subjects { get; set; }
    }
    public class StudentSubjectResultDto
    {
        public int SubjectId { get; set; }

        public string SubjectName { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal PassingMarks { get; set; }

        public decimal ObtainedMarks { get; set; }

        public string Status { get; set; }

        public string Remarks { get; set; }
    }
}
