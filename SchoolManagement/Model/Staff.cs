namespace SchoolManagement.Model
{
    public class Staff
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime DOB { get; set; }

        public int RoleId { get; set; }

        public DateTime DOJ { get; set; }

        public int SchoolId { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Adress { get; set; }

        public string Status { get; set; }

        public DateTime Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
    }
}
