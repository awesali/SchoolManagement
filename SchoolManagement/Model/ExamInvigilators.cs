// Backend section: domain and database models.
namespace SchoolManagement.Model
{
    // Represents exam invigilators domain data.
    public class ExamInvigilators
    {
        public int Id { get; set; }

        public int ExamScheduleId { get; set; }
        public int StaffId { get; set; }

        public string DutyType { get; set; }

        public ExamSchedules ExamSchedule { get; set; }
    }
}
