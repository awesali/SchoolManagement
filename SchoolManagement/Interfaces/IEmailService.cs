// Backend section: contracts for backend services and repositories.
namespace SchoolManagement.Service
{
    // Defines the i email operations used by the backend.
    public interface IEmailService
    {
        string LoginUrl { get; }
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments);
        Task SendEmailAsync(IReadOnlyCollection<string> recipients, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments);

        Task<(string subject, string body)> GetEmailTemplateAsync(
            string templateName,
            Dictionary<string, string> placeholders
        );
    }
}

namespace SchoolManagement.Service
{
    public record EmailAttachment(string FileName, byte[] Content, string MediaType = "application/pdf");
}
