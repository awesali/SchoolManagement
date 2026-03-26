namespace SchoolManagement.DTOs
{
    public class AddStaffDto
    {
        public string Name { get; set; }
        public DateTime DOB { get; set; }
        public DateTime DOJ { get; set; }

        public int RoleId { get; set; }
        public int SchoolId { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
