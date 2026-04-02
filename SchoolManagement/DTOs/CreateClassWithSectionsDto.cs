namespace SchoolManagement.DTOs
{
    public class CreateClassWithSectionsDto
    {
        public string ClassName { get; set; }
        public int SchoolId { get; set; }
        public List<SectionDto> Sections { get; set; }
    }

    public class SectionDto
    {
        public string SectionName { get; set; }
        public int StaffId { get; set; }
    }
}
