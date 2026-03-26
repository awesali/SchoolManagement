using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IAdminRepository
    {
        Task<Schools> CreateSchool(SchoolCreateDto dto, int userId);
        Task<DashboardCardDto> GetDashboardData(int schoolId);
        Task<List<Schools>> GetSchoolsBySuperAdminIdAsync(int superAdminId);
        Task<List<StaffListDto>> GetStaffFullAsync(int schoolId);
        Task<Staff> AddStaffAsync(AddStaffDto dto);
        Task<List<RoleDto>> GetRolesBySchoolIdAsync();

    }
}
