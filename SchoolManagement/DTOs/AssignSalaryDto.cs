namespace SchoolManagement.DTOs
{
    public class AssignSalaryDto
    {
        public int StaffId { get; set; }

        public decimal BasicSalary { get; set; }

        public string SalaryType { get; set; } = string.Empty;
    }
}
