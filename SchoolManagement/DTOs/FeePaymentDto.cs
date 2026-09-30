// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the fee payment request and response data.
    public class FeePaymentDto
    {
        public int StudentFeeId { get; set; }

        public decimal AmountPaid { get; set; }

        public string PaymentMode { get; set; }

        public string? AcknowledgementId { get; set; }

        public int SchoolId { get; set; }
    }
}
