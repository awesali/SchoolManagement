// Backend section: contracts for backend services and repositories.
namespace SchoolManagement.Service
{
    // Defines the i email operations used by the backend.
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        Task<(string subject, string body)> GetEmailTemplateAsync(
            string templateName,
            Dictionary<string, string> placeholders
        );
    }
}
