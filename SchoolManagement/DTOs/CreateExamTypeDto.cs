// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the create exam type request and response data.
    public class CreateExamTypeDto
    {
        public string Name { get; set; }
        public int SchoolId { get; set; }
    }
}
