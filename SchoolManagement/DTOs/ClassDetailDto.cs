namespace SchoolManagement.DTOs
{
    public class ClassDetailDto
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public int SchoolId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public int SectionCount { get; set; }

        // 🔥 Add this
        public List<GetSectionDto> Sections { get; set; }
    }

    public class GetSectionDto
    {
        public int Id { get; set; }
        public string SectionName { get; set; }
        public int StaffId { get; set; }
        public int? MonitorStudentId { get; set; }
    }
}
