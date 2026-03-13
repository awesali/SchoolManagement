namespace SchoolManagement.DTOs
{
    public class DashboardCardDto
    {
        public string TeachersPresentToday { get; set; }

        public string StudentsPresentToday { get; set; }

        public int TotalEmployees { get; set; }

        public int EmployeesOnLeave { get; set; }
    }
}
