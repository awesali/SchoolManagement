namespace SchoolManagement.DTOs
{
    public class CreateSessionDto
    {
        public int SchoolId { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
        public bool IsActive { get; set; }
    }
}
