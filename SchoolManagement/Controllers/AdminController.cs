using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _repo;

        public AdminController(IAdminRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateSchool(SchoolCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var result = await _repo.CreateSchool(dto, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DashboardCard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard([FromQuery] int schoolId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var data = await _repo.GetDashboardData(schoolId);

            return Ok(data);
        }

        [HttpGet("School-by-superadmin")]
        [Authorize]
        public async Task<IActionResult> GetSchoolsBySuperAdmin()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (superAdminId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SuperAdminId must be greater than 0"
                });
            }

            var schools = await _repo.GetSchoolsBySuperAdminIdAsync(superAdminId);

            if (schools == null || !schools.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No schools found"
                });
            }

            var result = schools.Select(s => new
            {
                s.Id,
                s.SchoolName,
                s.Address,
                s.Email,
                s.Phone,
                s.SuperAdminId,
                s.Created_Date
            });

            return Ok(new
            {
                success = true,
                count = result.Count(),
                data = result
            });
        }

        [HttpGet("Staff-by-school")]
        public async Task<IActionResult> GetStaffFull([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var data = await _repo.GetStaffFullAsync(schoolId);

            if (data == null || data.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No staff found"
                });
            }

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        [HttpPost("add-staff")]
        public async Task<IActionResult> AddStaff([FromBody] AddStaffDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data"
                });
            }

            if (dto.SchoolId <= 0 || dto.RoleId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SchoolId and RoleId are required"
                });
            }

            var result = await _repo.AddStaffAsync(dto);

            return Ok(new
            {
                success = true,
                message = "Staff added successfully",
                data = result
            });
        }

        [HttpGet("Get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _repo.GetRolesBySchoolIdAsync();

            if (roles == null || roles.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No roles found"
                });
            }

            return Ok(new
            {
                success = true,
                data = roles
            });
        }


        [HttpGet("students-by-school")]
        [Authorize]
        public async Task<IActionResult> GetStudentsBySchool([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var students = await _repo.GetStudentsBySchoolIdAsync(schoolId);

            if (students == null || !students.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No students found"
                });
            }

            return Ok(new
            {
                success = true,
                count = students.Count,
                data = students
            });
        }

        [HttpPost("add-student")]
        [Authorize]
        public async Task<IActionResult> AddStudent([FromBody] StudentCreateDto dto)
        {
            if (dto == null || dto.Parent == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input"
                });
            }

            var result = await _repo.AddStudentAsync(dto);

            if (!result)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to add student"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Student added successfully"
            });
        }

        [HttpGet("enrollment-info")]
        [Authorize]
        public async Task<IActionResult> GetEnrollmentInfo([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var enrollmentInfo = await _repo.GetEnrollmentInfoBySchoolAsync(schoolId);

            if (enrollmentInfo == null || !enrollmentInfo.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No enrollment data found"
                });
            }

            return Ok(new
            {
                success = true,
                count = enrollmentInfo.Count,
                data = enrollmentInfo
            });
        }
    }
}
