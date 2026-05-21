using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IStaffRepository
    {
        Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto);
        Task<List<StaffAttendanceHistoryDto>> GetStaffAttendanceHistoryAsync(DateTime fromDate, DateTime toDate, int schoolid);
        Task<StaffAttendanceNotificationDto> CheckTodayAttendanceAsync();
    }
}
