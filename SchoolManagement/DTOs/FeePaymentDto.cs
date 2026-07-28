namespace SchoolManagement.DTOs
{
    public class FeePaymentDto
    {
        public int StudentFeeId { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMode { get; set; }

        public string? AcknowledgementId { get; set; }

        public int SchoolId { get; set; }
    }
}
