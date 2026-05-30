namespace SchoolManagement.DTOs
{
    public class AssignFeeDto
    {
        public List<int> StudentIds { get; set; }

        public int FeeTypeId { get; set; }

        public decimal Amount { get; set; }

        public int SessionId { get; set; }

        public int SchoolId { get; set; }
    }
}
