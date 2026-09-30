// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the marks entry sheet request and response data.
    public class MarksEntrySheetDto
    {
        public int StudentId { get; set; }
        public int EnrollmentId { get; set; }

        public string StudentName { get; set; }

        public decimal? Marks { get; set; }
        public string RollNumber { get; set; }
        public string? Remarks { get; set; }
    }
}
