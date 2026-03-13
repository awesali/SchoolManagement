namespace SchoolManagement.Model
{
    public class Students
    {
        public int Id { get; set; }

        public string StudentName { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public int ParentId { get; set; }

        public int SchoolId { get; set; }

        public DateTime Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
    }
}
