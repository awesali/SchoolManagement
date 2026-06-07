namespace SchoolManagement.Model
{
    public class SalaryPayment
    {
        public int Id { get; set; }

        public int StaffId { get; set; }
        public int schoolId { get; set; }

        public int SalaryMonth { get; set; }

        public int SalaryYear { get; set; }

        public decimal BasicSalary { get; set; }

        public decimal Bonus { get; set; } = 0;

        public decimal Deduction { get; set; } = 0;

        public decimal NetSalary { get; set; }

        // Pending / Paid / Partial
        public string Status { get; set; } = "Pending";

        public DateTime? PaymentDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        public virtual Staff? Staff { get; set; }
    }
}
