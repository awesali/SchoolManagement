using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IStaffRepository
    {
        Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto);
        Task<List<StaffAttendanceHistoryDto>> GetStaffAttendanceHistoryAsync(DateTime fromDate, DateTime toDate, int schoolid);
        Task<StaffAttendanceNotificationDto> CheckTodayAttendanceAsync();
        Task<object> AssignSalary(AssignSalaryDto dto);

        Task<object> GenerateMonthlySalary(int month, int year);

        Task<object> PaySalary(PaySalaryDto dto);

        Task<object> GetSalaryHistory(int staffId);

        Task<object> GetPendingSalary();

        Task<object> GetPendingSalaryByStaff(int staffId);

        Task<object> GetDashboard(int schoolId);
    }
}
