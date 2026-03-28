namespace SchoolManagement.DTOs
{
    public class UpdateStaffDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public DateTime DOB { get; set; }
        public DateTime DOJ { get; set; }

        public int RoleId { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public bool IsActive { get; set; }

        public List<int?>? DocumentIds { get; set; }     // existing ids (optional)
        public List<string>? DocumentNames { get; set; } // names
        public List<IFormFile>? Files { get; set; }      // files
    }
}
