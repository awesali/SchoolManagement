// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the mark bulk attendance request and response data.
    public class MarkBulkAttendanceDto
    {
        public int SectionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public List<StudentAttendanceItemDto> Students { get; set; }
    }

    public class StudentAttendanceItemDto
    {
        public int StudentId { get; set; }
        public int EnrollmentId { get; set; }
        public string Status { get; set; } // Present / Absent / Leave
    }
}
