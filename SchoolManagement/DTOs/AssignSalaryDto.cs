// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the assign salary request and response data.
    public class AssignSalaryDto
    {
        public int StaffId { get; set; }

        public decimal BasicSalary { get; set; }

        public string SalaryType { get; set; } = string.Empty;

        public int SalaryGenerationDay { get; set; }

        public bool IsUpdate { get; set; }
    }
}
