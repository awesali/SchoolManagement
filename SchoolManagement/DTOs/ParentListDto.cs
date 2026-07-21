namespace SchoolManagement.DTOs
{
    public class ParentListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Relationship { get; set; }
        public bool IsActive { get; set; }
        public List<ParentStudentDto> Students { get; set; } = new();
    }

    public class ParentStudentDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string RollNumber { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
    }
}
