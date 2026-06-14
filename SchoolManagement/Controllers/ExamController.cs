using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Repository;
using System.Security.Claims;

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

        //[HttpPost("create-schedule")]
        //public async Task<IActionResult> CreateSchedule(CreateExamScheduleRequest request)
        //{
        //    await _repo.CreateExamSchedulesAsync(request);
        //    return Ok(new { message = "Exam schedules created successfully" });
        //}

        ////[HttpPost("schedule")]
        ////public async Task<IActionResult> AddSchedule(CreateExamScheduleDto dto)
        ////{
        ////    try
        ////    {
        ////        var ids = await _repo.AddExamScheduleAsync(dto);
        ////        return Ok(new ApiResponse<List<int>> { Success = true, Message = "Schedule created successfully", Data = ids });
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        ////    }
        ////}

        //[HttpPost("assign-invigilator")]
        //public async Task<IActionResult> AssignInvigilator(AssignInvigilatorDto dto)
        //{
        //    try
        //    {
        //        var id = await _repo.AssignInvigilatorAsync(dto);
        //        return Ok(new ApiResponse<int> { Success = true, Message = "Invigilator assigned successfully", Data = id });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
        //    }
        //}

        //[HttpGet("exam-type-picklist")]
        //public async Task<IActionResult> GetExamTypePicklist([FromQuery] int schoolId)
        //{
        //    var data = await _repo.GetExamTypePicklistAsync(schoolId);
        //    return Ok(new ApiResponse<List<ExamTypePicklistDto>> { Success = true, Data = data });
        //}

        //[HttpGet("Exam_detail")]
        //public async Task<IActionResult> GetExamDetail([FromQuery] int examId, [FromQuery] int schoolId)
        //{
        //    var data = await _repo.GetExamDetailAsync(examId, schoolId);
        //    if (data == null)
        //        return NotFound(new ApiResponse<string> { Success = false, Message = "Exam not found" });

        //    return Ok(new ApiResponse<ExamDetailDto> { Success = true, Data = data });
        //}

        //[HttpGet("scheduled-exams")]
        //public async Task<IActionResult> GetScheduledExams([FromQuery] int schoolId, int page = 1, int pageSize = 10)
        //{
        //    if (page == -1)
        //    {
        //        var (_, tempTotal) = await _repo.GetScheduledExamsAsync(schoolId, 1, pageSize);
        //        page = (int)Math.Ceiling((double)tempTotal / pageSize);
        //    }

        //    var (data, total) = await _repo.GetScheduledExamsAsync(schoolId, page, pageSize);
        //    var totalPages = (int)Math.Ceiling((double)total / pageSize);

        //    return Ok(new PagedResponse<List<ExamScheduleListDto>>
        //    {
        //        Success = true,
        //        Message = "Exam schedules fetched successfully",
        //        Data = data,
        //        CurrentPage = page,
        //        TotalPages = totalPages,
        //        TotalRecords = total,
        //        PageSize = pageSize
        //    });
        //}

        //[HttpPut("publish")]
        //public async Task<IActionResult> Publish([FromQuery] int examId)
        //{
        //    var result = await _repo.PublishExamAsync(examId);
        //    return result
        //        ? Ok(new ApiResponse<string> { Success = true, Message = "Exam published successfully" })
        //        : NotFound(new ApiResponse<string> { Success = false, Message = "Exam not found" });
        //}

        [HttpPost("CreateExamType")]
        public async Task<IActionResult> CreateExamType(CreateExamTypeDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.CreateExamType(dto, userId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet("GetExamTypes")]
        public async Task<IActionResult> GetExamTypes(int schoolId)
        {
            var result = await _repo.GetExamTypes(schoolId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("CreateExam")]
        public async Task<IActionResult> CreateExam(CreateExamDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.CreateExam(dto, userId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet("GetExams")]
        public async Task<IActionResult> GetExams(int schoolId)
        {
            var result = await _repo.GetExams(schoolId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPut("PublishExam")]
        public async Task<IActionResult> PublishExam(int examId)
        {
            var result = await _repo.PublishExam(examId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        [HttpPost("AddExamSubject")]
        public async Task<IActionResult>
           AddExamSubject(AddExamSubjectDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo
                .AddExamSubject(dto, userId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet("GetExamSubjects")]
        public async Task<IActionResult>
            GetExamSubjects(int examId)
        {
            var result = await _repo
                .GetExamSubjects(examId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("CreateExamSchedule")]
        public async Task<IActionResult> CreateExamSchedule([FromBody] CreateExamScheduleDto dto)
        {
            var result = await _repo.CreateExamSchedule(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetMarksEntrySheet")]
        public async Task<IActionResult> GetMarksEntrySheet(
          int schoolId,
          int examId,
          int sectionId,
          int subjectId)
        {
            int userId = int.Parse(
     User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.GetMarksEntrySheet(
                schoolId,
                examId,
                sectionId,
                subjectId,
                userId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("SaveMarks")]
        public async Task<IActionResult> SaveMarks(
            SaveMarksDto dto)
        {
            int userId = int.Parse(
     User.FindFirst(ClaimTypes.NameIdentifier)?.Value);


            var result = await _repo.SaveMarks(
                dto,
                userId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPut("LockMarks")]
        public async Task<IActionResult> LockMarks(
            int examId,
            int schoolId)
        {
            var result =
                await _repo.LockMarks(examId, schoolId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("GenerateResults")]
        public async Task<IActionResult> GenerateResults(GenerateResultDto dto)
        {
            var result = await _repo.GenerateResults(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetResults")]
        public async Task<IActionResult> GetResults(int examId, int schoolId)
        {
            var result = await _repo.GetResults(examId, schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("student")]
        public async Task<IActionResult> GetStudentResult(int studentId, int examId, int schoolId)
        {
            var result = await _repo.GetStudentResult(studentId, examId, schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("publish")]
        public async Task<IActionResult> Publish(int examId, int schoolId)
        {
            var result = await _repo.PublishResults(examId, schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpGet("student-result-detail")]
        public async Task<IActionResult> GetStudentResultDetail(int studentId,int examId,int schoolId)
        {
            var result = await _repo.GetStudentResultDetail(studentId,examId,schoolId);
            return Ok(result);
        }
    }
}
