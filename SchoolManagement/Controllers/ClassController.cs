// Backend section: HTTP endpoints and request handling.
using System.Security.Claims;
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
    // Exposes class HTTP endpoints and handles their requests.
    public class ClassController : ControllerBase
    {
        // Dependencies and state used by this component.
        private readonly IClassRepository _repo;

        // Creates the component with its required dependencies.
        public ClassController(IClassRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create-class-with-sections")]
        // API actions that validate requests and return responses.
        public async Task<IActionResult> CreateClassWithSections(CreateClassWithSectionsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.CreateClassWithSectionsAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("calss-list")]
        public async Task<IActionResult> GetClassList(int schoolId, int page = 1, int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var totalPages = 0;

            // Jump to last page: pass page = -1
            if (page == -1)
            {
                var (tempData, tempTotal) = await _repo.GetClassDetailsPagedAsync(
                    schoolId,
                    1,
                    pageSize
                );
                totalPages = (int)Math.Ceiling((double)tempTotal / pageSize);
                page = totalPages;
            }

            var (data, total) = await _repo.GetClassDetailsPagedAsync(schoolId, page, pageSize);
            totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(
                new PagedResponse<List<ClassDetailDto>>
                {
                    Success = true,
                    Message = "Class list fetched successfully",
                    Data = data,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalRecords = total,
                    PageSize = pageSize,
                }
            );
        }

        [HttpGet("my-classes")]
        public async Task<IActionResult> GetMyClasses()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var data = await _repo.GetTeacherClassesAsync(userId);
            return Ok(
                new ApiResponse<List<ClassDetailDto>>
                {
                    Success = true,
                    Message = "Assigned classes fetched successfully",
                    Data = data,
                }
            );
        }

        [HttpPut("update-class-with-sections")]
        public async Task<IActionResult> UpdateClassWithSections(UpdateClassWithSectionsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.UpdateClassWithSectionsAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("section-subjects/{sectionId}")]
        public async Task<IActionResult> GetSubjectsBySectionId(int sectionId)
        {
            var result = await _repo.GetSubjectsBySectionIdAsync(sectionId);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
