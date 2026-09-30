// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the teacher unit test list request and response data.
    public class TeacherUnitTestListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
        public DateTime TestDate { get; set; }
        public decimal MaxMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public bool CanEdit { get; set; }
    }
}
