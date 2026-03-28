namespace SchoolManagement.Model
{
    public class StaffDocument
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string DocumentName { get; set; } // 🔥 added
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
