// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the create session request and response data.
    public class CreateSessionDto
    {
        public int SchoolId { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
        public bool IsActive { get; set; }
    }
}
