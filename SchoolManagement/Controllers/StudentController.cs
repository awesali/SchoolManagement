using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using System.Globalization;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentRepository _repo;

        public StudentController(IStudentRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("add-student")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddStudent([FromForm] StudentCreateDto dto)
        {
            var result = await _repo.AddStudentAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update-student")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateStudent([FromForm] StudentUpdateDto dto)
        {
            var result = await _repo.UpdateStudentAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete-student-document")]
        public async Task<IActionResult> DeleteStudentDocument([FromQuery] int id)
        {
            var result = await _repo.DeleteStudentDocumentAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("students-by-school")]
        public async Task<IActionResult> GetStudentsBySchool([FromQuery] int schoolId, int page = 1, int pageSize = 10)
        {
            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetStudentsBySchoolIdAsync(schoolId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetStudentsBySchoolIdAsync(schoolId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<StudentDto>>
            {
                Success = true,
                Message = "Students fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpGet("student-by-id")]
        public async Task<IActionResult> GetStudentById([FromQuery] int studentId)
        {
            var result = await _repo.GetStudentByIdAsync(studentId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("enrollment-info")]
        public async Task<IActionResult> GetEnrollmentInfo([FromQuery] int schoolId)
        {
            var result = await _repo.GetEnrollmentInfoBySchoolAsync(schoolId);
            return Ok(result);
        }

        [HttpGet("GetStudentsBySection")]
        public async Task<IActionResult> GetMyStudents(int page = 1, int pageSize = 10)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetStudentsByTeacherIdAsync(teacherId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetStudentsByTeacherIdAsync(teacherId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<StudentDto>>
            {
                Success = true,
                Message = "Students fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpPost("StudentsAttendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkBulkAttendanceDto dto)
        {
            var result = await _repo.MarkAttendanceAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Student-attendance-history")]
        public async Task<IActionResult> GetAttendanceByDate(string date, int page = 1, int pageSize = 10)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!DateTime.TryParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Invalid date format. Use dd-MM-yyyy", Data = null });

            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetAttendanceHistoryAsync(teacherId, parsedDate, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetAttendanceHistoryAsync(teacherId, parsedDate, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<AttendanceHistoryDto>>
            {
                Success = true,
                Message = "Attendance history fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }
    }
}
