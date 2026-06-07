namespace SchoolManagement.Model
{
    public class StaffSalaryStructure
    {
        public int Id { get; set; }

        public int StaffId { get; set; }
        public int schoolId { get; set; }

        public decimal BasicSalary { get; set; }

        // Monthly / Daily / Hourly
        public string SalaryType { get; set; } = string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        public virtual Staff? Staff { get; set; }
    }
}
