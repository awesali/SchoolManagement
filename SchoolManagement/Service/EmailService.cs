using System.Net.Mail;
using System.Net;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Service
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public EmailService(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var fromEmail = _config["EmailSettings:FromEmail"];
            var password = _config["EmailSettings:Password"];
            var smtpHost = _config["EmailSettings:SmtpHost"];
            var port = int.Parse(_config["EmailSettings:Port"]);

            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = port,
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await smtpClient.SendMailAsync(mail);
        }

        public async Task<(string subject, string body)> GetEmailTemplateAsync(string templateName, Dictionary<string, string> placeholders)
        {
            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == templateName && t.IsActive);

            if (template == null)
                throw new Exception("Email template not found");

            var subject = template.Subject;
            var body = template.Body;

            // 🔥 Replace placeholders
            foreach (var key in placeholders.Keys)
            {
                body = body.Replace($"{{{{{key}}}}}", placeholders[key]);
                subject = subject.Replace($"{{{{{key}}}}}", placeholders[key]);
            }

            return (subject, body);
        }
    }
}
