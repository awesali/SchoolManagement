namespace SchoolManagement.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        Task<(string subject, string body)> GetEmailTemplateAsync(
            string templateName,
            Dictionary<string, string> placeholders
        );
    }
}