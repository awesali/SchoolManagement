namespace SchoolManagement.DTOs
{
    public class EnrollmentInfoDto
    {
       
    public List<ClassDto> Classes { get; set; }
        public List<SectionDto> Sections { get; set; }
        public List<SessionDto> Sessions { get; set; }
    }

    public class ClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SectionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ClassId { get; set; }
    }

    public class SessionDto
    {
        public int Id { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
    }

}
