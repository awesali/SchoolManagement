namespace SchoolManagement.DTOs
{
    public class AcademicSessionDto
    {
        public int Id { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
