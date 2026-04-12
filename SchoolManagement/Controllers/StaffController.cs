using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _repo;
        public StaffController(IStaffService repo)
        {
            repo = _repo;
        }
        [Authorize]
        [HttpPost("staff/mark-attendance")]
        public async Task<IActionResult> MarkStaffAttendance([FromBody] MarkStaffAttendanceDto dto)
        {
            var result = await _repo.MarkStaffAttendanceAsync(dto);
            return Ok(result);
        }
        [Authorize]
        [HttpGet("staff/attendance-history")]
        public async Task<IActionResult> GetStaffAttendanceHistory(DateTime fromDate, DateTime toDate)
        {
            var result = await _repo.GetStaffAttendanceHistoryAsync(fromDate, toDate);
            return Ok(result);
        }
    }
}
