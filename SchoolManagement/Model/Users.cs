// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents users domain data.
    public class Users
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Password_Hash { get; set; }

        public int? RoleId { get; set; }

        public int? School_Id { get; set; }

        public DateTime? Last_Login { get; set; }

        public bool Status { get; set; }

        public DateTime Created_At { get; set; }

        public bool IsActive { get; set; }
    }
}
