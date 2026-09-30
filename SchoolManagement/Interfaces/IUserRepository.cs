// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    // Defines the i user operations used by the backend.
    public interface IUserRepository
    {
        Task<Users> Register(RegisterDto dto);

        Task<string> Login(LoginDto dto);
    }
}
