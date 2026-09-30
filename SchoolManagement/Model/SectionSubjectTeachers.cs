// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents section subject teachers domain data.
    public class SectionSubjectTeachers
    {
        public int Id { get; set; }

        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int StaffId { get; set; }
        public int SchoolId { get; set; }

        public DateTime Created_Date { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
