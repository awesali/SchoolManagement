namespace SchoolManagement.Model
{
    public class Roles
    {
        public int Id { get; set; }

        public string RoleName { get; set; }

        public int? School_Id { get; set; }

        public DateTime Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
    }
}
