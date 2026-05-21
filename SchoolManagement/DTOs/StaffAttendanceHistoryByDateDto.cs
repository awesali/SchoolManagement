namespace SchoolManagement.DTOs
{
    public class StaffAttendanceHistoryByDateDto
    {
        public string StaffName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; }
    }
}
