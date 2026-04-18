namespace SchoolManagement.Model
{
    public class Exams
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int ExamTypeId { get; set; }

        public int SchoolId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsPublished { get; set; }

        // 🔥 Navigation Properties
    }
}
