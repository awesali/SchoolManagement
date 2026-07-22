namespace SchoolManagement.Model
{
    public class StudentFee
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int EnrollmentId { get; set; }

        public int FeeTypeId { get; set; }

        public decimal Amount { get; set; }

        public int SessionId { get; set; }

        public int SchoolId { get; set; }

        public DateTime? Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
        public string Status { get; set; }

        // Navigation Properties
        public virtual FeeType FeeType { get; set; }

        public virtual ICollection<FeePayments> FeePayments { get; set; }
    }
}
