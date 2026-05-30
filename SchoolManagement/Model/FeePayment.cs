namespace SchoolManagement.Model
{
    public class FeePayments
    {
        public int Id { get; set; }

        public int StudentFeeId { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime Payment_Date { get; set; }

        public string Payment_Mode { get; set; }

        public string Receipt_Number { get; set; }

        public DateTime? Created_Date { get; set; }

        public DateTime? Modified_Date { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public bool IsActive { get; set; }
        public int SchoolId { get; set; }

        // Navigation Property
        public virtual StudentFee StudentFee { get; set; }
    }
}
