namespace SchoolManagement.DTOs
{
    public class StudentResultDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }

        public string ExamName { get; set; }

        public List<SubjectResultDto> Subjects { get; set; }

        public decimal TotalMarks { get; set; }

        public decimal ObtainedMarks { get; set; }

        public decimal Percentage { get; set; }

        public string Grade { get; set; }

        public int Rank { get; set; }

        public string ResultStatus { get; set; }
    }
}
