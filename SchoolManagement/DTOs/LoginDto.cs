// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the login request and response data.
    public class LoginDto
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }
}
