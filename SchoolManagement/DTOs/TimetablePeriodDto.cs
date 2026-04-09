namespace SchoolManagement.DTOs
{
    public class TimetablePeriodDto
    {
        public int SectionId { get; set; }
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBreak { get; set; }
    }
}
