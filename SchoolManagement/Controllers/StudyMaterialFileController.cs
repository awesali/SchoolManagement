using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
public class StudyMaterialFileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private const long MaxUploadBytes = 100L * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg"
    };

    public StudyMaterialFileController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    [HttpPost("api/Teacher/study-materials/upload")]
    [RequestSizeLimit(MaxUploadBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] StudyMaterialUploadInput input)
    {
        if (User.FindFirstValue("RoleId") != "2" || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var staff = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == userId && x.IsActive);
        if (staff == null) return Forbid();
        if (!await _db.SectionSubjectTeachers.AnyAsync(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId &&
            x.SectionId == input.SectionId && x.SubjectId == input.SubjectId && x.IsActive)) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || input.Description?.Length > 1000 || input.File == null)
            return BadRequest(new { message = "Add a title and choose a file." });
        if (input.ResourceType is not ("PDF" or "Worksheet" or "Notes"))
            return BadRequest(new { message = "Choose a valid resource type." });
        var extension = Path.GetExtension(input.File.FileName).ToLowerInvariant();
        var allowed = input.ResourceType switch {
            "PDF" => new[] { ".pdf" },
            _ => new[] { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" }
        };
        if (input.File.Length == 0 || input.File.Length > MaxUploadBytes || !allowed.Contains(extension) ||
            !MimeTypes.TryGetValue(extension, out var mime) || !string.Equals(input.File.ContentType, mime, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Choose a supported file for this resource type (up to 100 MB)." });
        var folder = Path.Combine(_env.ContentRootPath, "private-uploads", "study-materials");
        Directory.CreateDirectory(folder);
        var storedName = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(folder, storedName);
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew)) await input.File.CopyToAsync(stream);
            var material = new TeacherStudyMaterial {
                SchoolId = staff.SchoolId, StaffId = staff.Id, SectionId = input.SectionId, SubjectId = input.SubjectId,
                Title = input.Title.Trim(), Description = input.Description?.Trim(), ResourceType = input.ResourceType,
                ResourceUrl = "upload:" + storedName
            };
            _db.TeacherStudyMaterials.Add(material);
            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { material.Id } });
        }
        catch
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            throw;
        }
    }

    [HttpGet("api/StudyMaterials/{id:int}/file")]
    public async Task<IActionResult> Download(int id)
    {
        var material = await _db.TeacherStudyMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (material == null || !material.ResourceUrl.StartsWith("upload:", StringComparison.Ordinal)) return NotFound();
        var allowed = false;
        if (User.FindFirstValue("RoleId") == "2" && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var teacherUserId))
            allowed = await _db.Staff.AnyAsync(x => x.usersid == teacherUserId && x.Id == material.StaffId && x.SchoolId == material.SchoolId && x.IsActive);
        else if (User.IsInRole("Student") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var credentialId))
        {
            var credential = await _db.Students_Parents_Creds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == credentialId && x.RoleName == "Student" && x.Status == "Active" && x.IsActive && x.School_Id == material.SchoolId);
            if (credential != null)
                allowed = await (from student in _db.Students
                    join enrollment in _db.StudentEnrollment on student.Id equals enrollment.StudentId
                    where student.Email == credential.Email && student.SchoolId == material.SchoolId && student.IsActive &&
                        enrollment.SchoolId == material.SchoolId && enrollment.SectionId == material.SectionId && enrollment.IsActive && enrollment.EnrollmentStatus == "Active"
                    select enrollment.Id).AnyAsync();
        }
        if (!allowed) return Forbid();
        var name = Path.GetFileName(material.ResourceUrl["upload:".Length..]);
        var extension = Path.GetExtension(name);
        if (!MimeTypes.TryGetValue(extension, out var mimeType)) return NotFound();
        var path = Path.Combine(_env.ContentRootPath, "private-uploads", "study-materials", name);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, mimeType, material.Title + extension);
    }
}

public class StudyMaterialUploadInput
{
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string ResourceType { get; set; } = "PDF";
    public IFormFile? File { get; set; }
}
