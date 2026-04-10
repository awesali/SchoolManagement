using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubjectController : ControllerBase
    {
        private readonly ISubjectRepository _repo;

        public SubjectController(ISubjectRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("subjects-by-school")]
        public async Task<IActionResult> GetSubjectsBySchool([FromQuery] int schoolId, int page = 1, int pageSize = 10)
        {
            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetSubjectsBySchoolIdAsync(schoolId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetSubjectsBySchoolIdAsync(schoolId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<SubjectDto>>
            {
                Success = true,
                Message = "Subjects fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpPost("add-subject")]
        public async Task<IActionResult> AddSubject(AddSubjectDto dto)
        {
            var result = await _repo.AddSubjectAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update-subject")]
        public async Task<IActionResult> UpdateSubject(UpdateSubjectDto dto)
        {
            var result = await _repo.UpdateSubjectAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("assign-subjects-to-section")]
        public async Task<IActionResult> AssignSubjectsToSection(AssignSubjectToSectionDto dto)
        {
            var result = await _repo.AssignSubjectsToSectionAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
