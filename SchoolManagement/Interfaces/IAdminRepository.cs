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
        Task<ApiResponse<Staff>> AddStaffAsync(AddStaffDto dto);
        Task<List<RoleDto>> GetRolesBySchoolIdAsync();
        Task<List<StudentDto>> GetStudentsBySchoolIdAsync(int schoolId);
        Task<StudentDto> GetStudentByIdAsync(int studentId);
        Task<bool> AddStudentAsync(StudentCreateDto dto);
        Task<EnrollmentInfoDto> GetEnrollmentInfoBySchoolAsync(int schoolId);
        Task<bool> UpdateStaffAsync(UpdateStaffDto dto);
        Task<bool> DeleteDocumentAsync(int documentId);
        Task<bool> UpdateStudentAsync(StudentUpdateDto dto);
        //Task<bool> DeleteStudentAsync(int studentId);
        Task<bool> DeleteStudentDocumentAsync(int documentId);
    }
}
