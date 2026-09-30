// Backend section: contracts for backend services and repositories.
using SchoolManagement.Model;

namespace SchoolManagement.Service
{
    // Defines the i jwt operations used by the backend.
    public interface IJwtService
    {
        string GenerateToken(Users user);

        string GenerateToken(string email, string roleName, int userId);
    }
}
