// Backend section: request and response data contracts.
namespace SchoolManagement.DTOs
{
    // Defines the account profile dtos request and response data.
    public class UpdateAccountProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
