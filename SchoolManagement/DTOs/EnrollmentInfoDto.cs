namespace SchoolManagement.DTOs
{
    public class EnrollmentInfoDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        public int SectionId { get; set; }
        public string SectionName { get; set; }

        public int SessionId { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
    }
}
