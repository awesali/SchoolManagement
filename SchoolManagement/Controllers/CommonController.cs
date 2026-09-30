// Backend section: HTTP endpoints and request handling.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // Exposes common HTTP endpoints and handles their requests.
    public class CommonController : ControllerBase
    {
        // Dependencies and state used by this component.
        private readonly ICommonRepository _common;

        // Creates the component with its required dependencies.
        public CommonController(ICommonRepository common)
        {
            _common = common;
        }

        [HttpGet("by-school/{schoolId}")]
        // API actions that validate requests and return responses.
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
