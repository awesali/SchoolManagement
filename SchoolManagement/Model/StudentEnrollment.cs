namespace SchoolManagement.Model
{
    public class StudentEnrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public int SchoolId { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;
        public DateTime? Updated_Date { get; set; }

        public int Created_By { get; set; }
        public int Updated_By { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
