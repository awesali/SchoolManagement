namespace SchoolManagement.Model
{
    public class ExamSubjects
    {
        public int Id { get; set; }

        public int SchoolId { get; set; }

        public int ExamId { get; set; }

        public int ClassId { get; set; }

        public int? SectionId { get; set; }

        public int SubjectId { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal PassingMarks { get; set; }

        public DateTime Created_Date { get; set; }

        public bool IsActive { get; set; }
    }
}
