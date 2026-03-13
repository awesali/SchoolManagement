using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IUserRepository
    {
        Task<Users> Register(RegisterDto dto);

        Task<string> Login(LoginDto dto);
    }
}
