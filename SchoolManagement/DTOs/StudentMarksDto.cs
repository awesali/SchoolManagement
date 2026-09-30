// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the student marks request and response data.
    public class StudentMarksDto
    {
        public int StudentId { get; set; }
        public int EnrollmentId { get; set; }

        public decimal ObtainedMarks { get; set; }

        public string? Remarks { get; set; }
    }
}
