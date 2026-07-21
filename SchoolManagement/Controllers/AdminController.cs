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

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
        {
            var result = await _repo.CreateAcademicSessionAsync(dto);
            return Ok(result);
        }

        [HttpGet("parents-by-school")]
        public async Task<IActionResult> GetParentsBySchool(
            [FromQuery] int schoolId,
            int page = 1,
            int pageSize = 10,
            string? search = null)
        {
            if (schoolId <= 0)
                return BadRequest("A valid schoolId is required.");

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (data, total) = await _repo.GetParentsBySchoolAsync(schoolId, page, pageSize, search);

            return Ok(new PagedResponse<List<ParentListDto>>
            {
                Success = true,
                Message = "Parents fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpGet("academic-sessions")]
        public async Task<IActionResult> GetAcademicSessions([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new ApiResponse<List<AcademicSessionDto>>
                {
                    Success = false,
                    Message = "A valid schoolId is required",
                    Data = new List<AcademicSessionDto>()
                });
            }

            var result = await _repo.GetAcademicSessionsAsync(schoolId);
            return Ok(result);
        }

        [HttpGet("GetStaffAttendanceBySchool")]
        public async Task<IActionResult> GetAttendanceBySchool(int schoolId)
        {
            var result = await _repo.GetStaffAttendanceBySchoolAsync(schoolId);

            if (result == null || !result.Any())
            {
                return NotFound("No attendance records found.");
            }

            return Ok(result);
        }

        [HttpGet("GetStaffAttendanceHistoryByDate")]
        public async Task<IActionResult> GetAttendanceHistory(
        int schoolId,
        DateTime fromDate,
        DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return BadRequest("From date cannot be greater than To date.");
            }

            var result = await _repo.GetAttendanceHistoryAsync(
                schoolId,
                fromDate,
                toDate);

            if (!result.Any())
            {
                return NotFound("No attendance records found.");
            }

            return Ok(result);
        }
    }
}
