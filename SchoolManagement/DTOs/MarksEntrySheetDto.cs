namespace SchoolManagement.DTOs
{
    public class MarksEntrySheetDto
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; }

        public decimal? Marks { get; set; }
        public string RollNumber { get; set; }
        public string? Remarks { get; set; }
    }
}
