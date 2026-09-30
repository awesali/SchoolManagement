// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents student_documents domain data.
    public class Student_Documents
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string DocumentName { get; set; }
    }
}
