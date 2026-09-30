// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the timetable period request and response data.
    public class TimetablePeriodDto
    {
        public int SectionId { get; set; }
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBreak { get; set; }
    }
}
