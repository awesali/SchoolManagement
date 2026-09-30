// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the generate result request and response data.
    public class GenerateResultDto
    {
        public int SchoolId { get; set; }

        public int ExamId { get; set; }
    }
}
