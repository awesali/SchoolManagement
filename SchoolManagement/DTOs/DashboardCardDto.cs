// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the dashboard card request and response data.
    public class DashboardCardDto
    {
        public string TeachersPresentToday { get; set; }

        public string StudentsPresentToday { get; set; }

        public int TotalEmployees { get; set; }

        public int EmployeesOnLeave { get; set; }
    }
}
