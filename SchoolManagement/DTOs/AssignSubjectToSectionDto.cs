// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the assign subject to section request and response data.
    public class AssignSubjectToSectionDto
    {
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<int> SubjectIds { get; set; }
    }
}
