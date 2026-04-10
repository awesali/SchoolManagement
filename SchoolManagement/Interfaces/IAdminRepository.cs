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
    }
}
