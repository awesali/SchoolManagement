// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the update subject request and response data.
    public class UpdateSubjectDto
    {
        public int Id { get; set; }
        public string SubjectName { get; set; }
        public int StaffId { get; set; }
    }
}
