using SchoolManagement.Model;

namespace SchoolManagement.Service
{
    public interface IJwtService
    {
        string GenerateToken(Users user);

        string GenerateToken(string email, string roleName, int userId);
    }
}