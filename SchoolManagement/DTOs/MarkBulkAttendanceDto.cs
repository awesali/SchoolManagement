namespace SchoolManagement.DTOs
{
    public class MarkBulkAttendanceDto
    {
        public int SectionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public List<StudentAttendanceItemDto> Students { get; set; }

    }
    public class StudentAttendanceItemDto
    {
        public int StudentId { get; set; }
        public string Status { get; set; } // Present / Absent / Leave
    }
}
