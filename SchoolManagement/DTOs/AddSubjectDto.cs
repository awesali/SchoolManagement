// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the add subject request and response data.
    public class AddSubjectDto
    {
        public string SubjectName { get; set; }
        public int SchoolId { get; set; }
        public int StaffId { get; set; }
    }
}
