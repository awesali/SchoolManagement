namespace SchoolManagement.DTOs
{
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
