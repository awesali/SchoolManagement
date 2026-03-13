using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IAdminRepository
    {
        Task<Schools> CreateSchool(SchoolCreateDto dto, int userId);
        Task<DashboardCardDto> GetDashboardData(int schoolId);


    }
}
