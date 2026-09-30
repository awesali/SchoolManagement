// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the staff attendance notification request and response data.
    public class StaffAttendanceNotificationDto
    {
        public bool ShouldMarkAttendance { get; set; }
        public string Message { get; set; }
    }
}
