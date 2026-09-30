// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the subject request and response data.
    public class SubjectDto
    {
        public int Id { get; set; }
        public string SubjectName { get; set; }
        public int SchoolId { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? Modified_Date { get; set; }
        public bool IsActive { get; set; }
        public int? TeacherId { get; set; }
        public string TeacherName { get; set; }
    }
}
