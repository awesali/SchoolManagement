namespace SchoolManagement.DTOs
{
    public class EnrollmentInfoDto
    {
       
    public List<ClassDto> Classes { get; set; }
        public List<SectionDetailsDto> Sections { get; set; }
        public List<SessionDto> Sessions { get; set; }
    }

    public class ClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SectionDetailsDto
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
