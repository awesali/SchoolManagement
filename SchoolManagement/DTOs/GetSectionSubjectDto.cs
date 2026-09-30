// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the get section subject request and response data.
    public class GetSectionSubjectDto
    {
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
    }
}
