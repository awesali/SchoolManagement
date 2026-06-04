namespace SchoolManagement.Model
{
    public class ExamMarks
    {
        public int Id { get; set; }

        public int SchoolId { get; set; }
        public int ExamId { get; set; }
        public int ExamScheduleId { get; set; }
        public int StudentId { get; set; }

        public decimal ObtainedMarks { get; set; }
        public string? Remarks { get; set; }

        public int EnteredBy { get; set; }

        public DateTime Created_Date { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime EnteredDate { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsLocked { get; set; }
    }
}
