using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentParentAuthController : ControllerBase
    {
        private readonly IStudentParentRepository _studentParentRepo;

        public StudentParentAuthController(IStudentParentRepository studentParentRepo)
        {
            _studentParentRepo = studentParentRepo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] StudentParentRegisterDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input"
                });
            }

            var result = await _studentParentRepo.RegisterStudentParentAsync(dto);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email already exists or registration failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Registration successful"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StudentParentLoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input"
                });
            }

            try
            {
                var token = await _studentParentRepo.LoginStudentParentAsync(dto);

                return Ok(new
                {
                    success = true,
                    token = token,
                    message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized"
                });
            }

            var user = await _studentParentRepo.GetByEmailAsync(email);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phone = user.Phone,
                    roleName = user.RoleName,
                    schoolId = user.School_Id,
                    status = user.Status,
                    lastLogin = user.Last_Login,
                    createdAt = user.Created_At,
                    isActive = user.IsActive
                }
            });
        }
    }
}
