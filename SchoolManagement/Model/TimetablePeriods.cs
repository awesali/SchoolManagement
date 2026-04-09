using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Model
{
    [Table("TimetablePeriods")]
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
