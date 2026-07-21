using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IAdminRepository
    {
        Task<ApiResponse<Schools>> CreateSchool(SchoolCreateDto dto, int userId);

        Task<ApiResponse<DashboardCardDto>> GetDashboardData(int schoolId);

        Task<ApiResponse<List<Schools>>> GetSchoolsBySuperAdminIdAsync(int superAdminId);

        Task<(List<StaffListDto> Data, int TotalRecords)> GetStaffFullAsync(int schoolId, int page, int pageSize);

        Task<ApiResponse<string>> DeleteDocumentAsync(int documentId);

        Task<ApiResponse<Staff>> AddStaffAsync(AddStaffDto dto);

        Task<ApiResponse<string>> UpdateStaffAsync(UpdateStaffDto dto);

        Task<ApiResponse<List<RoleDto>>> GetRolesBySchoolIdAsync();
        Task<ApiResponse<string>> CreateAcademicSessionAsync(CreateSessionDto dto);
        Task<ApiResponse<List<AcademicSessionDto>>> GetAcademicSessionsAsync(int schoolId);
        Task<(List<ParentListDto> Data, int TotalRecords)> GetParentsBySchoolAsync(int schoolId, int page, int pageSize, string? search);
        Task<List<StaffAttendanceDto>> GetStaffAttendanceBySchoolAsync(int schoolId);
        Task<List<StaffAttendanceHistoryByDateDto>> GetAttendanceHistoryAsync(int schoolId,DateTime fromDate,DateTime toDate);
    }
}
