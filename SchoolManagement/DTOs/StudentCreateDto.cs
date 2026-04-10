namespace SchoolManagement.DTOs
{
    public class StudentCreateDto
    {
        public string StudentName { get; set; }
        public DateTime DOB { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public string Rollnumber { get; set; }

        public ParentDto Parent { get; set; }
        public List<int?>? DocumentIds { get; set; }
        public List<string>? DocumentNames { get; set; }  // ✅ names
        public List<IFormFile>? Files { get; set; }
    }
    public class ParentDto
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Relationship { get; set; } // Father, Mother, etc.
    }
}
