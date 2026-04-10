using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClassController : ControllerBase
    {
        private readonly IClassRepository _repo;

        public ClassController(IClassRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create-class-with-sections")]
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
                var (tempData, tempTotal) = await _repo.GetClassDetailsPagedAsync(schoolId, 1, pageSize);
                totalPages = (int)Math.Ceiling((double)tempTotal / pageSize);
                page = totalPages;
            }

            var (data, total) = await _repo.GetClassDetailsPagedAsync(schoolId, page, pageSize);
            totalPages = (int)Math.Ceiling((double)total / pageSize);

            return Ok(new PagedResponse<List<ClassDetailDto>>
            {
                Success = true,
                Message = "Class list fetched successfully",
                Data = data,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = total,
                PageSize = pageSize
            });
        }

        [HttpPut("update-class-with-sections")]
        public async Task<IActionResult> UpdateClassWithSections(UpdateClassWithSectionsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _repo.UpdateClassWithSectionsAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
