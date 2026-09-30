// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the staff attendance history by date request and response data.
    public class StaffAttendanceHistoryByDateDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; }
    }
}
