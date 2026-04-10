using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _repo;

        public AdminController(IAdminRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSchool(SchoolCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.CreateSchool(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("DashboardCard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int schoolId)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetDashboardData(schoolId);
            return Ok(result);
        }

        [HttpGet("School-by-superadmin")]
        public async Task<IActionResult> GetSchoolsBySuperAdmin()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetSchoolsBySuperAdminIdAsync(superAdminId);
            return Ok(result);
        }

        [HttpGet("Staff-by-school")]
        public async Task<IActionResult> GetStaffFull([FromQuery] int schoolId, int page = 1, int pageSize = 10)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetStaffFullAsync(schoolId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetStaffFullAsync(schoolId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<StaffListDto>>
            {
                Success = true,
                Message = "Staff fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpPost("add-staff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddStaff([FromForm] AddStaffDto dto)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.AddStaffAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateStaff([FromForm] UpdateStaffDto dto)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.UpdateStaffAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromQuery] int id)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.DeleteDocumentAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("Get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetRolesBySchoolIdAsync();
            return Ok(result);
        }
    }
}
