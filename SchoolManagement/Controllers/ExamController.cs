using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExamController : ControllerBase
    {
        private readonly IExamRepository _repo;

        public ExamController(IExamRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create-schedule")]
        public async Task<IActionResult> CreateSchedule(CreateExamScheduleRequest request)
        {
            await _repo.CreateExamSchedulesAsync(request);
            return Ok(new { message = "Exam schedules created successfully" });
        }

        //[HttpPost("schedule")]
        //public async Task<IActionResult> AddSchedule(CreateExamScheduleDto dto)
        //{
        //    try
        //    {
        //        var ids = await _repo.AddExamScheduleAsync(dto);
        //        return Ok(new ApiResponse<List<int>> { Success = true, Message = "Schedule created successfully", Data = ids });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        //    }
        //}

        [HttpPost("assign-invigilator")]
        public async Task<IActionResult> AssignInvigilator(AssignInvigilatorDto dto)
        {
            try
            {
                var id = await _repo.AssignInvigilatorAsync(dto);
                return Ok(new ApiResponse<int> { Success = true, Message = "Invigilator assigned successfully", Data = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("exam-type-picklist")]
        public async Task<IActionResult> GetExamTypePicklist([FromQuery] int schoolId)
        {
            var data = await _repo.GetExamTypePicklistAsync(schoolId);
            return Ok(new ApiResponse<List<ExamTypePicklistDto>> { Success = true, Data = data });
        }

        [HttpGet("Exam_detail")]
        public async Task<IActionResult> GetExamDetail([FromQuery] int examId, [FromQuery] int schoolId)
        {
            var data = await _repo.GetExamDetailAsync(examId, schoolId);
            if (data == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "Exam not found" });

            return Ok(new ApiResponse<ExamDetailDto> { Success = true, Data = data });
        }

        [HttpGet("scheduled-exams")]
        public async Task<IActionResult> GetScheduledExams([FromQuery] int schoolId, int page = 1, int pageSize = 10)
        {
            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetScheduledExamsAsync(schoolId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetScheduledExamsAsync(schoolId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<ExamScheduleListDto>>
            {
                Success = true,
                Message = "Exam schedules fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        //[HttpPut("publish")]
        //public async Task<IActionResult> Publish([FromQuery] int examId)
        //{
        //    var result = await _repo.PublishExamAsync(examId);
        //    return result
        //        ? Ok(new ApiResponse<string> { Success = true, Message = "Exam published successfully" })
        //        : NotFound(new ApiResponse<string> { Success = false, Message = "Exam not found" });
        //}
    }
}
