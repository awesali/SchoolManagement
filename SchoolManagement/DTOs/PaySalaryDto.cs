// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the pay salary request and response data.
    public class PaySalaryDto
    {
        public List<SalaryPaymentItemDto> Salaries { get; set; } = new();
    }

    public class SalaryPaymentItemDto
    {
        public int StaffId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Bonus { get; set; }

        public decimal Deduction { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string? PaymentReference { get; set; }

        public string? Remarks { get; set; }
    }
}
