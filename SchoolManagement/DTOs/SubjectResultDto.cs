// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the subject result request and response data.
    public class SubjectResultDto
    {
        public string SubjectName { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal ObtainedMarks { get; set; }

        public decimal PassingMarks { get; set; }
    }
}
