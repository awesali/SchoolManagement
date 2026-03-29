namespace SchoolManagement.Model
{
    public class SectionDetails
    {
        public int Id { get; set; }

        public string SectionName { get; set; }

        public int ClassId { get; set; }
        public int StaffId { get; set; }
        public int MonitorStudentId { get; set; }
        public int SchoolId { get; set; }

        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime? Modified_Date { get; set; }

        public int Created_By { get; set; }
        public int Updated_By { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
