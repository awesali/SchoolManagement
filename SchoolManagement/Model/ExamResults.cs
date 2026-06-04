namespace SchoolManagement.Model
{
    public class ExamResults
    {
        public int Id { get; set; }

        public int SchoolId { get; set; }

        public int ExamId { get; set; }

        public int StudentId { get; set; }

        public decimal TotalMarks { get; set; }

        public decimal ObtainedMarks { get; set; }

        public decimal Percentage { get; set; }

        public string Grade { get; set; }

        public int RankPosition { get; set; }

        public string ResultStatus { get; set; } // PASS / FAIL

        public bool Published { get; set; }

        public DateTime Created_Date { get; set; } = DateTime.Now;
    }
}
