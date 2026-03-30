using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IStudentParentRepository
    {
        Task<bool> RegisterStudentParentAsync(StudentParentRegisterDto dto);
        Task<string> LoginStudentParentAsync(StudentParentLoginDto dto);
        Task<Students_Parents_Creds> GetByEmailAsync(string email);
        Task<bool> UpdateLastLoginAsync(int id);
    }
}
