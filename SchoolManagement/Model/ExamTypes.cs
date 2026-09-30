// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents exam types domain data.
    public class ExamTypes
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }

        public int schoolId { get; set; }
    }
}
