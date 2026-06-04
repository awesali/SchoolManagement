namespace SchoolManagement.Model
{
    public class SectionSubjectTeachers
    {
        public int Id { get; set; }

        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int StaffId { get; set; }
        public int SchoolId { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
