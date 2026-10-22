// Backend section: application services and shared rules.
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;

namespace SchoolManagement.Service
{
    // Implements email application behavior.
    public class EmailService : IEmailService
    {
        // Dependencies and state used by this component.
        private readonly IConfiguration _config;
        public string LoginUrl => _config["EmailSettings:LoginUrl"]?.Trim() ?? "https://academicrigor.cognerasystems.com/";
        private readonly AppDbContext _context;

        // Creates the component with its required dependencies.
        public EmailService(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public Task SendEmailAsync(string toEmail, string subject, string body) =>
            SendEmailAsync(toEmail, subject, body, Array.Empty<EmailAttachment>());

        public Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments) =>
            SendEmailAsync(new[] { toEmail }, subject, body, attachments);

        public async Task SendEmailAsync(IReadOnlyCollection<string> recipients, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments)
        {
            Exception? lastError = null;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var fromEmail = _config["EmailSettings:FromEmail"]?.Trim();
                    var password = _config["EmailSettings:Password"]?.Trim();
                    var smtpHost = _config["EmailSettings:SmtpHost"]?.Trim();
                    var port = int.Parse(_config["EmailSettings:Port"]!);
                    if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password) ||
                        string.IsNullOrWhiteSpace(smtpHost))
                        throw new InvalidOperationException("Email settings are incomplete.");

                    var mail = new MimeMessage();
                    mail.From.Add(new MailboxAddress("Academic Rigor", fromEmail));
                    mail.Subject = subject;
                    foreach (var recipient in recipients.Distinct(StringComparer.OrdinalIgnoreCase))
                        mail.To.Add(MailboxAddress.Parse(recipient));
                    var builder = new BodyBuilder { HtmlBody = body };
                    foreach (var attachment in attachments)
                        builder.Attachments.Add(attachment.FileName, attachment.Content,
                            new ContentType(attachment.MediaType.Split('/')[0], attachment.MediaType.Split('/')[1]));
                    mail.Body = builder.ToMessageBody();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var smtpClient = new SmtpClient();
                    await smtpClient.ConnectAsync(smtpHost, port,
                        port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                        timeout.Token);
                    await smtpClient.AuthenticateAsync(fromEmail, password, timeout.Token);
                    await smtpClient.SendAsync(mail, timeout.Token);
                    await smtpClient.DisconnectAsync(true, timeout.Token);
                    return;
                }
                catch (Exception exception)
                {
                    lastError = exception;
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                }
            }

            throw new InvalidOperationException(
                "Email delivery failed after three attempts.",
                lastError
            );
        }

        public async Task<(string subject, string body)> GetEmailTemplateAsync(
            string templateName,
            Dictionary<string, string> placeholders
        )
        {
            var template = await _context.EmailTemplates.FirstOrDefaultAsync(t =>
                t.TemplateName == templateName && t.IsActive
            );

            if (template == null)
                throw new Exception("Email template not found");

            var subject = template.Subject;
            var body = template.Body;

            // 🔥 Replace placeholders
            foreach (var key in placeholders.Keys)
            {
                body = body.Replace($"{{{{{key}}}}}", System.Net.WebUtility.HtmlEncode(placeholders[key]));
                subject = subject.Replace($"{{{{{key}}}}}", placeholders[key]);
            }

            return (subject, body);
        }
    }
}
