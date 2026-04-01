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
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? SessionId { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public DateTime? AcademicSession { get; set; }
        public bool IsActive { get; set; }

        public List<StudentDocumentDto> Documents { get; set; }
    }

    public class StudentDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentURL { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
