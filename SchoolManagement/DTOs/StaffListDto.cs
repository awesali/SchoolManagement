namespace SchoolManagement.DTOs
{
    public class StaffListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DOB { get; set; }
        public DateTime DOJ { get; set; }

        public int RoleId { get; set; }
        public string RoleName { get; set; }

        public string SchoolName { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }

        public List<StaffDocumentDto> Documents { get; set; }
    }

    public class StaffDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentURL { get; set; }
    }
}
