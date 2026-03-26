namespace SchoolManagement.DTOs
{
    public class StaffListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime? DOJ { get; set; }

        // Role
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        // School
        public string SchoolName { get; set; }
    }
}
