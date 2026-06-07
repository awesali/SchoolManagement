using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repo;
        public StaffController(IStaffRepository repo)
        {
            _repo = repo;
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
        public async Task<IActionResult> GetStaffAttendanceHistory(DateTime fromDate, DateTime toDate, int schoolid)
        {
            var result = await _repo.GetStaffAttendanceHistoryAsync(fromDate, toDate, schoolid);
            return Ok(result);
        }

        [HttpGet("check-attendance")]
        public async Task<IActionResult> CheckAttendance()
        {
            var result = await _repo.CheckTodayAttendanceAsync();
            return Ok(result);
        }
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSalary(AssignSalaryDto dto)
        {
            return Ok(await _repo.AssignSalary(dto));
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate(int month, int year)
        {
            return Ok(
                await _repo.GenerateMonthlySalary(month, year));
        }

        [HttpPost("pay")]
        public async Task<IActionResult> Pay(PaySalaryDto dto)
        {
            return Ok(await _repo.PaySalary(dto));
        }

        [HttpGet("history/{staffId}")]
        public async Task<IActionResult> History(int staffId)
        {
            return Ok(
                await _repo.GetSalaryHistory(staffId));
        }

        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(await _repo.GetPendingSalary());
        }

        [HttpGet("pending/{staffId}")]
        public async Task<IActionResult> PendingByStaff(int staffId)
        {
            return Ok(
                await _repo.GetPendingSalaryByStaff(staffId));
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(int schoolId)
        {
            return Ok(await _repo.GetDashboard(schoolId));
        }
    }
}
