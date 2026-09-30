// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the academic session request and response data.
    public class AcademicSessionDto
    {
        public int Id { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateAcademicSessionStatusDto
    {
        public int SchoolId { get; set; }
        public int SessionId { get; set; }
        public bool IsActive { get; set; }
    }
}
