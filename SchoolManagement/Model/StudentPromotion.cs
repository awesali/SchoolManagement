namespace SchoolManagement.Model
{
    public class StudentPromotion
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int FromEnrollmentId { get; set; }
        public int? ToEnrollmentId { get; set; }
        public int FromSessionId { get; set; }
        public int? ToSessionId { get; set; }
        public int FromClassId { get; set; }
        public int? ToClassId { get; set; }
        public int FromSectionId { get; set; }
        public int? ToSectionId { get; set; }
        public string PromotionType { get; set; } = "Pass";
        public DateTime PromotionDate { get; set; } = DateTime.UtcNow;
        public int SchoolId { get; set; }
        public int CreatedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
