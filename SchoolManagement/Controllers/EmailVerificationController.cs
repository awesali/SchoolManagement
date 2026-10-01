using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Service;

namespace SchoolManagement.Controllers;

[ApiController]
[Route("api/email-verification")]
public sealed class EmailVerificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    public EmailVerificationController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string? token)
    {
        if (!EmailVerificationLinks.TryRead(_configuration, token, out var claim))
            return BadRequest(new { message = "This verification link is invalid or has expired." });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        if (claim.Kind == "Staff")
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == claim.Id && x.School_Id == claim.SchoolId && x.Email.ToLower() == claim.Email);
            if (user == null) return NotFound(new { message = "Account was not found." });
            var staff = await _db.Staff.FirstOrDefaultAsync(x => x.usersid == user.Id && x.SchoolId == claim.SchoolId && x.Email.ToLower() == claim.Email);
            if (staff == null) return NotFound(new { message = "Staff account was not found." });
            if (staff.Status != "PendingVerification") return Conflict(new { message = "This link has already been used or the account is no longer pending verification." });
            user.IsActive = true;
            staff.IsActive = true;
            staff.Status = "Active";
        }
        else
        {
            var credential = await _db.Students_Parents_Creds.FirstOrDefaultAsync(x => x.Id == claim.Id && x.School_Id == claim.SchoolId && x.Email.ToLower() == claim.Email && x.RoleName == claim.Kind);
            if (credential == null) return NotFound(new { message = "Account was not found." });
            if (credential.Status != "PendingVerification") return Conflict(new { message = "This link has already been used or the account is no longer pending verification." });
            credential.IsActive = true;
            credential.Status = "Active";
            if (claim.Kind == "Student")
            {
                var students = await _db.Students.Where(x => x.SchoolId == claim.SchoolId && x.Email.ToLower() == claim.Email).Take(2).ToListAsync();
                if (students.Count != 1) return Conflict(new { message = "Student account could not be linked. Contact your school." });
                students[0].IsActive = true;
            }
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Content("Email verified. Your account is now active. You can sign in.", "text/plain");
    }
}
