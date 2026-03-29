namespace SchoolManagement.Model
{
    public class AcademicSessions
    {
        public int Id { get; set; }

        public int SchoolId { get; set; }

        public DateTime Year_Start { get; set; }
        public DateTime Year_End { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;
        public int Created_By { get; set; }
        public int? Updated_By { get; set; }
        public bool IsAcitve { get; set; } = true;
    }
}
