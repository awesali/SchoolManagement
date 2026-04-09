namespace SchoolManagement.DTOs
{
    public class UpdateTimetableDto
    {
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<TimetablePeriodDto> Periods { get; set; }
        public List<DayTimeTableDto> Days { get; set; }
    }
}
