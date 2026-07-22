namespace SchoolManagement.DTOs
{
    public class StudentMarksDto
    {
        public int StudentId { get; set; }
        public int EnrollmentId { get; set; }

        public decimal ObtainedMarks { get; set; }

        public string? Remarks { get; set; }
    }
}
