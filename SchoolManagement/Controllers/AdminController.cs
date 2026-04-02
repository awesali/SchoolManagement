using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _repo;

        public AdminController(IAdminRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateSchool(SchoolCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var result = await _repo.CreateSchool(dto, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("DashboardCard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard([FromQuery] int schoolId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var data = await _repo.GetDashboardData(schoolId);

            return Ok(data);
        }

        [HttpGet("School-by-superadmin")]
        [Authorize]
        public async Task<IActionResult> GetSchoolsBySuperAdmin()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (superAdminId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SuperAdminId must be greater than 0"
                });
            }

            var schools = await _repo.GetSchoolsBySuperAdminIdAsync(superAdminId);

            if (schools == null || !schools.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No schools found"
                });
            }

            var result = schools.Select(s => new
            {
                s.Id,
                s.SchoolName,
                s.Address,
                s.Email,
                s.Phone,
                s.SuperAdminId,
                s.Created_Date
            });

            return Ok(new
            {
                success = true,
                count = result.Count(),
                data = result
            });
        }

        [HttpGet("Staff-by-school")]
        public async Task<IActionResult> GetStaffFull([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var data = await _repo.GetStaffFullAsync(schoolId);

            if (data == null || data.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No staff found"
                });
            }

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        [HttpPost("add-staff")]
        [Consumes("multipart/form-data")] // 🔥 MUST ADD
        public async Task<IActionResult> AddStaff([FromForm] AddStaffDto dto)
        {
            var result = await _repo.AddStaffAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateStaff([FromForm] UpdateStaffDto dto)
        {
            var result = await _repo.UpdateStaffAsync(dto);

            if (!result)
                return NotFound(new { message = "Staff not found" });

            return Ok(new { message = "Staff updated successfully" });
        }

        [HttpPut("update-student")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateStudent([FromForm] StudentUpdateDto dto)
        {
            var result = await _repo.UpdateStudentAsync(dto);

            if (!result)
                return NotFound(new { message = "Student not found" });

            return Ok(new { message = "Student updated successfully" });
        }

        [HttpDelete("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromQuery] int id)
        {
            var result = await _repo.DeleteDocumentAsync(id);

            if (!result)
                return NotFound(new { message = "Document not found" });

            return Ok(new { message = "Document deleted successfully" });
        }

        //[HttpDelete("delete-student")]
        //public async Task<IActionResult> DeleteStudent([FromQuery] int id)
        //{
        //    var result = await _repo.DeleteStudentAsync(id);

        //    if (!result)
        //        return NotFound(new { message = "Student not found" });

        //    return Ok(new { message = "Student deleted successfully" });
        //}

        [HttpDelete("delete-student-document")]
        public async Task<IActionResult> DeleteStudentDocument([FromQuery] int id)
        {
            var result = await _repo.DeleteStudentDocumentAsync(id);

            if (!result)
                return NotFound(new { message = "Student document not found" });

            return Ok(new { message = "Student document deleted successfully" });
        }
        [HttpGet("Get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _repo.GetRolesBySchoolIdAsync();

            if (roles == null || roles.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No roles found"
                });
            }

            return Ok(new
            {
                success = true,
                data = roles
            });
        }


        [HttpGet("students-by-school")]
        [Authorize]
        public async Task<IActionResult> GetStudentsBySchool([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var students = await _repo.GetStudentsBySchoolIdAsync(schoolId);

            if (students == null || !students.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No students found"
                });
            }

            return Ok(new
            {
                success = true,
                count = students.Count,
                data = students
            });
        }

        [HttpGet("student-by-id")]
        [Authorize]
        public async Task<IActionResult> GetStudentById([FromQuery] int studentId)
        {
            if (studentId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid studentId"
                });
            }

            var student = await _repo.GetStudentByIdAsync(studentId);

            if (student == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Student not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = student
            });
        }

        [HttpPost("add-student")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddStudent([FromForm] StudentCreateDto dto)
        {
            if (dto == null || dto.Parent == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input"
                });
            }

            var result = await _repo.AddStudentAsync(dto);

            if (!result)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to add student"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Student added successfully"
            });
        }

        [HttpGet("enrollment-info")]
        [Authorize]
        public async Task<IActionResult> GetEnrollmentInfo([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid schoolId"
                });
            }

            var enrollmentInfo = await _repo.GetEnrollmentInfoBySchoolAsync(schoolId);

            if (enrollmentInfo == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No enrollment data found"
                });
            }

            return Ok(new
            {
                success = true,
                data = enrollmentInfo
            });
        }

        [HttpPost("create-class-with-sections")]
        public async Task<IActionResult> CreateClassWithSections(CreateClassWithSectionsDto dto)
        {
            var result = await _repo.CreateClassWithSectionsAsync(dto);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to create class"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Class with sections created successfully"
            });
        }

        [HttpGet("calss-list")]
        public async Task<IActionResult> GetClassList(int schoolId)
        {
            var data = await _repo.GetClassDetailsBySchoolIdAsync(schoolId);

            var response = new ApiResponse<List<ClassDetailDto>>
            {
                Success = true,
                Message = "Class list fetched successfully",
                Data = data
            };

            return Ok(response);
        }

        [HttpPut("update-class-with-sections")]
        public async Task<IActionResult> UpdateClassWithSections(UpdateClassWithSectionsDto dto)
        {
            var result = await _repo.UpdateClassWithSectionsAsync(dto);

            if (!result)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Class and sections updated successfully",
                Data = null
            });
        }

        [HttpGet("subjects-by-school")]
        public async Task<IActionResult> GetSubjectsBySchool([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Invalid schoolId" });

            var subjects = await _repo.GetSubjectsBySchoolIdAsync(schoolId);

            if (subjects == null || !subjects.Any())
                return NotFound(new ApiResponse<string> { Success = false, Message = "No subjects found" });

            return Ok(new ApiResponse<List<SubjectDto>>
            {
                Success = true,
                Message = "Subjects fetched successfully",
                Data = subjects
            });
        }

        [HttpPost("add-subject")]
        public async Task<IActionResult> AddSubject(AddSubjectDto dto)
        {
            try
            {
                var subject = await _repo.AddSubjectAsync(dto);

                return Ok(new ApiResponse<SubjectDto>
                {
                    Success = true,
                    Message = "Subject added successfully",
                    Data = subject
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPut("update-subject")]
        public async Task<IActionResult> UpdateSubject(UpdateSubjectDto dto)
        {
            try
            {
                var result = await _repo.UpdateSubjectAsync(dto);

                if (!result)
                    return NotFound(new ApiResponse<string> { Success = false, Message = "Subject not found" });

                return Ok(new ApiResponse<string> { Success = true, Message = "Subject updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("assign-subjects-to-section")]
        public async Task<IActionResult> AssignSubjectsToSection(AssignSubjectToSectionDto dto)
        {
            try
            {
                var result = await _repo.AssignSubjectsToSectionAsync(dto);

                if (!result)
                    return NotFound(new ApiResponse<string> { Success = false, Message = "Section not found" });

                return Ok(new ApiResponse<string> { Success = true, Message = "Subjects assigned to section successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }
    }
}
