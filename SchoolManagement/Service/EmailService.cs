using System.Net.Mail;
using System.Net;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Service
{
    public class EmailService : IEmailService
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
            Exception? lastError = null;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var fromEmail = _config["EmailSettings:FromEmail"]?.Trim();
                    var password = _config["EmailSettings:Password"]?.Replace(" ", "").Trim();
                    var smtpHost = _config["EmailSettings:SmtpHost"]?.Trim();
                    var port = int.Parse(_config["EmailSettings:Port"]!);

                    using var smtpClient = new SmtpClient(smtpHost, port)
                    {
                        EnableSsl = true,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromEmail, password),
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(fromEmail!),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mail.To.Add(toEmail);
                    await smtpClient.SendMailAsync(mail);
                    return;
                }
                catch (Exception exception)
                {
                    lastError = exception;
                    if (attempt < 3) await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }

            throw new InvalidOperationException($"Email delivery to {toEmail} failed after three attempts.", lastError);
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
