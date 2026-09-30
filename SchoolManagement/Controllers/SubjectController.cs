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
    // Exposes subject HTTP endpoints and handles their requests.
    public class SubjectController : ControllerBase
    {
        // Dependencies and state used by this component.
        private readonly ISubjectRepository _repo;

        // Creates the component with its required dependencies.
        public SubjectController(ISubjectRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("subjects-by-school")]
        // API actions that validate requests and return responses.
        public async Task<IActionResult> GetSubjectsBySchool(
            [FromQuery] int schoolId,
            int page = 1,
            int pageSize = 10
        )
        {
            if (page == -1)
            {
                var (_, tempTotal) = await _repo.GetSubjectsBySchoolIdAsync(schoolId, 1, pageSize);
                page = (int)Math.Ceiling((double)tempTotal / pageSize);
            }

            var (data, total) = await _repo.GetSubjectsBySchoolIdAsync(schoolId, page, pageSize);
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(
                new PagedResponse<List<SubjectDto>>
                {
                    Success = true,
                    Message = "Subjects fetched successfully",
                    Data = data,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalRecords = total,
                    PageSize = pageSize,
                }
            );
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

        [HttpGet("GetSubjectsBySection")]
        public async Task<IActionResult> GetSubjectsBySection(int sectionId, int schoolId)
        {
            var result = await _repo.GetSubjectsBySectionAsync(sectionId, schoolId);

            if (result == null || !result.Any())
            {
                return NotFound("No subjects found for this section");
            }

            return Ok(result);
        }
    }
}
