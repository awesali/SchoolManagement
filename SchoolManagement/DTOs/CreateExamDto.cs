namespace SchoolManagement.DTOs
{
    public class CreateExamDto
    {
        public string Name { get; set; }

        public int ExamTypeId { get; set; }

        public int SchoolId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
