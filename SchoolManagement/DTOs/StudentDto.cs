namespace SchoolManagement.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public DateTime DOB { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int ParentId { get; set; }
        public int SchoolId { get; set; }

        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public DateTime? AcademicSession { get; set; }
    }
}
