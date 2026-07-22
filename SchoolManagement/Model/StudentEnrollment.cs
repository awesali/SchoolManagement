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
        public string? RollNumber { get; set; }
        public string AdmissionType { get; set; } = "New";
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public string PromotionStatus { get; set; } = "NotProcessed";
        public string EnrollmentStatus { get; set; } = "Active";

        public DateTime Created_At { get; set; } = DateTime.UtcNow;
        public DateTime? Updated_Date { get; set; }

        public int Created_By { get; set; }
        public int Updated_By { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
