namespace SchoolManagement.Model
{
    public class StaffAttendance
    {
        public int Id { get; set; }

        public int Staff_Id { get; set; }

        public DateTime Attendance_Date { get; set; }

        public string Status { get; set; }

        public int? School_Id { get; set; }

        public DateTime Created_At { get; set; }

        public int? Created_By { get; set; }

        public int? Updated_By { get; set; }

        public DateTime? Updated_Date { get; set; }

        public bool IsActive { get; set; }
    }
}
