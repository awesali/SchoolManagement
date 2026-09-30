// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the mark staff attendance request and response data.
    public class MarkStaffAttendanceDto
    {
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; }
    }

    public class StaffAttendanceHistoryDto
    {
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; }
    }
}
