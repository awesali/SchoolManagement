// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the update timetable request and response data.
    public class UpdateTimetableDto
    {
        public int SectionId { get; set; }
        public int SchoolId { get; set; }
        public List<TimetablePeriodDto> Periods { get; set; }
        public List<DayTimeTableDto> Days { get; set; }
    }
}
