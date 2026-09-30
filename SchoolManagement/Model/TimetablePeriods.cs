// Backend section: domain and database models.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagement.Model
{
    [Table("TimetablePeriods")]
    // Represents timetable periods domain data.
    public class TimetablePeriods
    {
        [Key]
        public int Id { get; set; }
        public int SectionId { get; set; }
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBreak { get; set; }
    }
}
