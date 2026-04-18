namespace SchoolManagement.DTOs
{
    public class AssignInvigilatorDto
    {
        public int ExamScheduleId { get; set; }
        public int StaffId { get; set; }
        public string DutyType { get; set; } // Main / Assistant
    }
}
