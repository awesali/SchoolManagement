using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommonController : ControllerBase
    {
        private readonly ICommonRepository _common;
        public CommonController(ICommonRepository common)
        {
            _common = common;
        }

        [HttpGet("by-school/{schoolId}")]
        public async Task<IActionResult> GetStaffBySchool(int schoolId)
        {
            var data = await _common.GetStaffBySchoolIdAsync(schoolId);
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }

        [HttpGet("subjects/{schoolId}")]
        public async Task<IActionResult> GetSubjectsBySchool(int schoolId)
        {
            var data = await _common.GetSubjectsBySchoolIdAsync(schoolId);
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }
    }
}
