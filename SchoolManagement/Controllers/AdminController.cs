using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Service;
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
        [Consumes("multipart/form-data")] // 🔥 MUST ADD
        public async Task<IActionResult> AddStaff([FromForm] AddStaffDto dto)
        {
            var result = await _repo.AddStaffAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateStaff([FromForm] UpdateStaffDto dto)
        {
            var result = await _repo.UpdateStaffAsync(dto);

            if (!result)
                return NotFound(new { message = "Staff not found" });

            return Ok(new { message = "Staff updated successfully" });
        }

        [HttpDelete("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromQuery] int id)
        {
            var result = await _repo.DeleteDocumentAsync(id);

            if (!result)
                return NotFound(new { message = "Document not found" });

            return Ok(new { message = "Document deleted successfully" });
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
    }
}
