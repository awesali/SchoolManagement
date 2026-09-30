// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the create teacher unit test request and response data.
    public class CreateTeacherUnitTestDto
    {
        public string Name { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public DateTime TestDate { get; set; }
        public decimal MaxMarks { get; set; }
        public decimal PassingMarks { get; set; }
    }
}
