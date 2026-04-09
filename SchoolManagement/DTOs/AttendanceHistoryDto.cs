namespace SchoolManagement.DTOs
{
    public class AttendanceHistoryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; }
    }
}
