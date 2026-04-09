namespace SchoolManagement.Model
{
    public class Timetables
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public int DayOfWeek { get; set; } // 1=Mon
        public int PeriodId { get; set; }
        public int? SubjectId { get; set; }
        public int SchoolId { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? Updated_Date { get; set; }

        // Navigation
        public Subjects Subject { get; set; }
    }
}
