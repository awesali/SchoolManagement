namespace SchoolManagement.DTOs
{
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

        public string? Remarks { get; set; }
    }
}
