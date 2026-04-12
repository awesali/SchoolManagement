using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface IStaffService 
    {
        Task<ApiResponse<string>> MarkStaffAttendanceAsync(MarkStaffAttendanceDto dto);
        Task<List<StaffAttendanceHistoryDto>> GetStaffAttendanceHistoryAsync(DateTime fromDate, DateTime toDate);
    }
}
