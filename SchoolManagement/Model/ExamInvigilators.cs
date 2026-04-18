namespace SchoolManagement.Model
{
    public class ExamInvigilators
    {
        public int Id { get; set; }

        public int ExamScheduleId { get; set; }
        public int StaffId { get; set; }

        public string DutyType { get; set; }

        public ExamSchedules ExamSchedule { get; set; }
    }
}
