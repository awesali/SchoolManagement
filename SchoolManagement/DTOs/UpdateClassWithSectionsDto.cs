// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the update class with sections request and response data.
    public class UpdateClassWithSectionsDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int UpdatedBy { get; set; }

        public List<UpdateSectionDto> Sections { get; set; }
    }

    public class UpdateSectionDto
    {
        public int? Id { get; set; } // null = new section
        public string SectionName { get; set; }
        public int StaffId { get; set; }
        public int? MonitorStudentId { get; set; }
    }
}
