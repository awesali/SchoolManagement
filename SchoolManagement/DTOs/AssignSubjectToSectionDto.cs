namespace SchoolManagement.DTOs
{
    public class AssignSubjectToSectionDto
    {
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<int> SubjectIds { get; set; }
    }
}
