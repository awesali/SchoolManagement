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
        Task<object> GetAssignedSalary(int staffId);

        Task<object> GenerateMonthlySalary(int month, int year, int schoolId);

        Task<object> PaySalary(PaySalaryDto dto);

        Task<object> GetSalaryHistory(int staffId);
        Task<object> GetSalaryHistoryByPeriod(int schoolId, int month, int year);

        Task<object> GetPendingSalary(int schoolId);

        Task<object> GetPendingSalaryByStaff(int staffId);

        Task<object> GetDashboard(int schoolId);
    }
}
