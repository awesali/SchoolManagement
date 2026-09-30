// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents classes domain data.
    public class Classes
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public int SchoolId { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? Modified_Date { get; set; }
        public bool IsActive { get; set; }
    }
}
