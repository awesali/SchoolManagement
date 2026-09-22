using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AuthController(IUserRepository repo, AppDbContext context, IWebHostEnvironment env)
        {
            _repo = repo;
            _context = context;
            _env = env;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
            if (user == null) return NotFound(new { message = "User profile not found." });
            var staffId = await _context.Staff.AsNoTracking()
                .Where(x => x.usersid == userId).Select(x => (int?)x.Id).FirstOrDefaultAsync();
            var picture = await _context.ProfilePictures.AsNoTracking()
                .Where(x => x.IsActive && ((x.PersonType == "User" && x.PersonId == userId) ||
                    (staffId.HasValue && x.PersonType == "Staff" && x.PersonId == staffId.Value)))
                .OrderByDescending(x => x.PersonType == "User").ThenByDescending(x => x.CreatedDate)
                .Select(x => x.FileUrl).FirstOrDefaultAsync();
            var roleName = await _context.Roles.AsNoTracking().Where(x => x.Id == user.RoleId).Select(x => x.RoleName).FirstOrDefaultAsync();
            // Resolve teacher branding from the authenticated staff record, never a requested school ID.
            var profileSchoolId = user.RoleId == 2
                ? await _context.Staff.AsNoTracking().Where(x => x.usersid == userId && x.IsActive)
                    .Select(x => (int?)x.SchoolId).FirstOrDefaultAsync()
                : user.School_Id;
            var schoolName = await _context.Schools.AsNoTracking().Where(x => x.Id == profileSchoolId)
                .Select(x => x.SchoolName).FirstOrDefaultAsync();
            var schoolLogoUrl = await _context.ProfilePictures.AsNoTracking()
                .Where(x => x.PersonType == "School" && x.PersonId == profileSchoolId && x.IsActive)
                .OrderByDescending(x => x.CreatedDate).Select(x => x.FileUrl).FirstOrDefaultAsync();
            return Ok(new { user.Id, user.Name, user.Email, user.Phone, RoleName = roleName, SchoolName = schoolName, SchoolLogoUrl = schoolLogoUrl, ProfilePictureUrl = picture });
        }

        [Authorize]
        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateAccountProfileDto dto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            var name = dto.Name?.Trim();
            var phone = dto.Phone?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { success = false, message = "Name is required." });
            if (name.Length > 150) return BadRequest(new { success = false, message = "Name cannot exceed 150 characters." });
            if (phone.Length > 0 && (phone.Length != 10 || phone.Any(x => !char.IsDigit(x))))
                return BadRequest(new { success = false, message = "Phone number must contain 10 digits." });
            if (dto.ProfilePicture != null && (!new[] { "image/jpeg", "image/png", "image/webp" }.Contains(dto.ProfilePicture.ContentType.ToLowerInvariant()) || dto.ProfilePicture.Length == 0 || dto.ProfilePicture.Length > 5 * 1024 * 1024))
                return BadRequest(new { success = false, message = "Choose a JPG, PNG, or WebP image up to 5 MB." });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
            if (user == null) return NotFound(new { success = false, message = "User profile not found." });
            user.Name = name;
            user.Phone = phone;
            var staff = await _context.Staff.FirstOrDefaultAsync(x => x.usersid == userId && x.IsActive);
            if (staff != null) { staff.Name = name; staff.Phone = phone; staff.Modified_Date = DateTime.UtcNow; }

            ProfilePicture? oldPicture = null;
            ProfilePicture? newPicture = null;
            if (dto.ProfilePicture != null)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var folder = Path.Combine(webRoot, "uploads", "user-profiles");
                Directory.CreateDirectory(folder);
                var extension = dto.ProfilePicture.ContentType.ToLowerInvariant() switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
                var fileName = $"user-{userId}-{Guid.NewGuid():N}{extension}";
                await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
                    await dto.ProfilePicture.CopyToAsync(stream);
                newPicture = new ProfilePicture { PersonType = "User", PersonId = userId, FileName = fileName,
                    FileUrl = $"/uploads/user-profiles/{fileName}", ContentType = dto.ProfilePicture.ContentType,
                    CreatedDate = DateTime.UtcNow, IsActive = true };
                oldPicture = await _context.ProfilePictures.Where(x => x.PersonType == "User" && x.PersonId == userId && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                if (oldPicture != null) oldPicture.IsActive = false;
                _context.ProfilePictures.Add(newPicture);
            }
            try { await _context.SaveChangesAsync(); }
            catch
            {
                if (newPicture != null)
                {
                    var failed = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), newPicture.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(failed)) System.IO.File.Delete(failed);
                }
                throw;
            }
            if (oldPicture != null)
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), oldPicture.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            var roleName = await _context.Roles.AsNoTracking().Where(x => x.Id == user.RoleId).Select(x => x.RoleName).FirstOrDefaultAsync();
            var schoolName = await _context.Schools.AsNoTracking().Where(x => x.Id == user.School_Id).Select(x => x.SchoolName).FirstOrDefaultAsync();
            var pictureUrl = newPicture?.FileUrl ?? await _context.ProfilePictures.AsNoTracking()
                .Where(x => x.IsActive && ((x.PersonType == "User" && x.PersonId == userId) || (staff != null && x.PersonType == "Staff" && x.PersonId == staff.Id)))
                .OrderByDescending(x => x.PersonType == "User").ThenByDescending(x => x.CreatedDate).Select(x => x.FileUrl).FirstOrDefaultAsync();
            return Ok(new { success = true, message = "Profile updated successfully.", data = new { user.Id, user.Name, user.Email, user.Phone, RoleName = roleName, SchoolName = schoolName, ProfilePictureUrl = pictureUrl } });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            if (string.IsNullOrEmpty(dto.CurrentPassword)) return BadRequest(new { success = false, message = "Current password is required." });
            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 8 || dto.NewPassword.Length > 128)
                return BadRequest(new { success = false, message = "New password must contain 8 to 128 characters." });
            if (dto.NewPassword != dto.ConfirmPassword) return BadRequest(new { success = false, message = "New password and confirmation do not match." });
            if (dto.CurrentPassword == dto.NewPassword) return BadRequest(new { success = false, message = "New password must be different from the current password." });
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Status);
            if (user == null) return NotFound(new { success = false, message = "User account not found." });
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password_Hash))
                return BadRequest(new { success = false, message = "Current password is incorrect." });
            user.Password_Hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Password changed successfully." });
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                var result = await _repo.Register(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var token = await _repo.Login(dto);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
