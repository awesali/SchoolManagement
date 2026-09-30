// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    // Defines the i student parent operations used by the backend.
    public interface IStudentParentRepository
    {
        Task<bool> RegisterStudentParentAsync(StudentParentRegisterDto dto);
        Task<string> LoginStudentParentAsync(StudentParentLoginDto dto);
        Task<Students_Parents_Creds> GetByEmailAsync(string email);
        Task<bool> UpdateLastLoginAsync(int id);
    }
}
