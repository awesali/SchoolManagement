// Backend section: HTTP endpoints and request handling.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // Exposes timetable HTTP endpoints and handles their requests.
    public class TimetableController : ControllerBase
    {
        // Dependencies and state used by this component.
        private readonly ITimetableRepository _repo;

        // Creates the component with its required dependencies.
        public TimetableController(ITimetableRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("save-timetable")]
        // API actions that validate requests and return responses.
        public async Task<IActionResult> SaveTimetable(SaveTimetableDto dto)
        {
            var result = await _repo.SaveTimetableAsync(dto);

            if (!result)
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Failed to save timetable",
                    }
                );

            return Ok(
                new ApiResponse<string> { Success = true, Message = "Timetable saved successfully" }
            );
        }

        [HttpGet("get-timetable")]
        public async Task<IActionResult> GetTimetable(int sectionId)
        {
            var data = await _repo.GetTimetableAsync(sectionId);
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }

        [HttpPut("update-timetable")]
        public async Task<IActionResult> UpdateTimetable(UpdateTimetableDto dto)
        {
            try
            {
                var result = await _repo.UpdateTimetableAsync(dto);

                if (!result)
                    return BadRequest(
                        new ApiResponse<string>
                        {
                            Success = false,
                            Message = "Failed to update timetable",
                        }
                    );

                return Ok(
                    new ApiResponse<string>
                    {
                        Success = true,
                        Message = "Timetable updated successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse<string> { Success = false, Message = ex.Message }
                );
            }
        }
    }
}
