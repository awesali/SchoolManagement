namespace SchoolManagement.DTOs
{
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
