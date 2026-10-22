using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Service;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/support-requests")]
public sealed class SupportRequestsController : ControllerBase
{
    private const string TemplateName = "SupportRequest";
    private static readonly string[] Recipients =
        { "awesalisayed3@gmail.com", "faisalmirzzaa12@gmail.com" };
    private static readonly ConcurrentDictionary<string, (DateTimeOffset Start, int Count)> RequestsByUser = new();
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SupportRequestsController> _logger;

    public SupportRequestsController(AppDbContext db, IEmailService email, ILogger<SupportRequestsController> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] SupportRequestInput input)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var subject = input.Subject?.Trim() ?? "";
        var problem = input.Problem?.Trim() ?? "";
        if (subject.Length < 3 || subject.Length > 150 || subject.Any(char.IsControl) ||
            problem.Length < 10 || problem.Length > 4000)
            return BadRequest(new { message = "Enter a subject and a problem description." });

        IReadOnlyCollection<EmailAttachment> attachments = Array.Empty<EmailAttachment>();
        if (input.Screenshot != null)
        {
            if (input.Screenshot.Length == 0 || input.Screenshot.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Screenshot must be an image up to 5 MB." });
            var extension = Path.GetExtension(input.Screenshot.FileName).ToLowerInvariant();
            var mediaType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => null
            };
            if (mediaType == null)
                return BadRequest(new { message = "Upload a PNG, JPG or WebP screenshot." });
            using var buffer = new MemoryStream();
            await input.Screenshot.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            var valid = mediaType switch
            {
                "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
                "image/webp" => bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP",
                _ => false
            };
            if (!valid) return BadRequest(new { message = "The screenshot file is not a valid image." });
            attachments = new[] { new EmailAttachment("support-screenshot" + extension, bytes, mediaType) };
        }

        var isStudent = string.Equals(User.FindFirstValue(ClaimTypes.Role), "Student", StringComparison.OrdinalIgnoreCase);
        string name;
        string email;
        string role;
        int schoolId;
        if (isStudent)
        {
            var credential = await _db.Students_Parents_Creds.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Status == "Active" && x.RoleName == "Student");
            if (credential == null) return Unauthorized();
            email = credential.Email;
            schoolId = credential.School_Id;
            name = await _db.Students.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsActive && x.Email == email)
                .Select(x => x.StudentName).FirstOrDefaultAsync() ?? email;
            role = "Student";
        }
        else
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
            if (user == null) return Unauthorized();
            name = user.Name;
            email = user.Email;
            schoolId = user.School_Id ?? 0;
            role = await _db.Roles.AsNoTracking().Where(x => x.Id == user.RoleId)
                .Select(x => x.RoleName).FirstOrDefaultAsync() ?? "Staff";
            var staffSchool = await _db.Staff.AsNoTracking()
                .Where(x => x.usersid == userId && x.IsActive)
                .Select(x => (int?)x.SchoolId).FirstOrDefaultAsync();
            if (staffSchool.HasValue) schoolId = staffSchool.Value;
        }
        var schoolName = await _db.Schools.AsNoTracking().Where(x => x.Id == schoolId)
            .Select(x => x.SchoolName).FirstOrDefaultAsync() ?? "Academic Rigor";
        var key = $"{(isStudent ? "student" : "staff")}:{userId}";
        var now = DateTimeOffset.UtcNow;
        var usage = RequestsByUser.AddOrUpdate(key, (now, 1), (_, previous) =>
            now - previous.Start >= TimeSpan.FromHours(1) ? (now, 1) : (previous.Start, previous.Count + 1));
        if (usage.Count > 5)
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Too many support requests. Please try again later." });
        try
        {
            var (mailSubject, body) = await _email.GetEmailTemplateAsync(TemplateName, new()
            {
                ["Name"] = name, ["Role"] = role, ["SchoolName"] = schoolName,
                ["Email"] = email, ["Subject"] = subject, ["Problem"] = problem
            });
            await _email.SendEmailAsync(Recipients, mailSubject, body, attachments);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send support request email for user {UserId}.", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Support request could not be sent right now. Please try again." });
        }
    }
}

public sealed class SupportRequestInput
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = "";

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Problem { get; set; } = "";

    public IFormFile? Screenshot { get; set; }
}
