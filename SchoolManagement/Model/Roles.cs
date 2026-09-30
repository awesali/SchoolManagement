// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents roles domain data.
    public class Roles
    {
        public int Id { get; set; }

        public string RoleName { get; set; }

        public string? Description { get; set; }

        public int? School_Id { get; set; }

        public DateTime Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
    }
}
