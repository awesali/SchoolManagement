using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Service;

namespace SchoolManagement.Controllers;

[ApiController]
[Route("api/demo-requests")]
public sealed class DemoRequestsController : ControllerBase
{
    private const string Recipient = "awesalisayed3@gmail.com";
    private const string TemplateName = "DemoRequest";
    private static readonly ConcurrentDictionary<string, (DateTimeOffset Start, int Count)> RequestsByIp = new();
    private readonly IEmailService _email;
    private readonly ILogger<DemoRequestsController> _logger;

    public DemoRequestsController(IEmailService email, ILogger<DemoRequestsController> logger)
    {
        _email = email;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DemoRequestInput input)
    {
        // The hidden website field is filled by simple bots, not by regular visitors.
        if (!string.IsNullOrWhiteSpace(input.Website))
            return Ok(new { success = true });

        if (HasControlCharacters(input.Name, input.SchoolName, input.Email, input.Phone))
            return BadRequest(new { message = "Enter valid contact details." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow;
        var usage = RequestsByIp.AddOrUpdate(ip, (now, 1), (_, previous) =>
            now - previous.Start >= TimeSpan.FromHours(1) ? (now, 1) : (previous.Start, previous.Count + 1));
        if (usage.Count > 5)
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Too many requests. Please try again later." });

        try
        {
            var (subject, body) = await _email.GetEmailTemplateAsync(TemplateName,
                new Dictionary<string, string>
                {
                    ["Name"] = input.Name.Trim(),
                    ["SchoolName"] = input.SchoolName.Trim(),
                    ["Email"] = input.Email.Trim(),
                    ["Phone"] = input.Phone.Trim(),
                    ["Message"] = string.IsNullOrWhiteSpace(input.Message) ? "Not provided" : input.Message.Trim(),
                });
            await _email.SendEmailAsync(Recipient, subject, body);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send demo request email.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Your request could not be sent right now. Please try again later." });
        }
    }

    private static bool HasControlCharacters(params string[] values) =>
        values.Any(value => value.Any(char.IsControl));
}

public sealed class DemoRequestInput
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, StringLength(150, MinimumLength = 2)]
    public string SchoolName { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(@"^[+0-9()\s.-]{7,20}$")]
    public string Phone { get; set; } = "";

    [StringLength(1000)]
    public string? Message { get; set; }

    public string? Website { get; set; }
}
