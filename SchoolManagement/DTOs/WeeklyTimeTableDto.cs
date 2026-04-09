namespace SchoolManagement.DTOs
{
    public class SaveTimetableDto
    {
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<TimetablePeriodDto> Periods { get; set; }
        public List<DayTimeTableDto> Days { get; set; }
    }

    public class DayTimeTableDto
    {
        public int DayOfWeek { get; set; }
        public List<PeriodSlotDto> Periods { get; set; }
    }

    public class PeriodSlotDto
    {
        public int PeriodId { get; set; }
        public int? SubjectId { get; set; }
    }
}
