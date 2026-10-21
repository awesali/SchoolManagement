// Backend section: HTTP endpoints and request handling.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // Exposes admin HTTP endpoints and handles their requests.
    public class AdminController : ControllerBase
    {
        // Dependencies and state used by this component.
        private readonly IAdminRepository _repo;
        private readonly AppDbContext _db;

        // Creates the component with its required dependencies.
        public AdminController(IAdminRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        // API actions that validate requests and return responses.
        public async Task<IActionResult> CreateSchool([FromForm] SchoolCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.CreateSchool(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("DashboardCard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int schoolId)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetDashboardData(schoolId);
            return Ok(result);
        }

        [HttpGet("dashboard-exams")]
        public async Task<IActionResult> DashboardExams(int schoolId)
        {
            var today = DateTime.Today;
            var cutoff = DateTime.UtcNow.AddDays(-2);
            var notices = await _db.Exams.AsNoTracking()
                .Where(e => e.SchoolId == schoolId && e.ResultPublished && e.CreatedDate >= cutoff)
                .OrderByDescending(e => e.CreatedDate).ThenByDescending(e => e.Id)
                .Select(e => new { e.Id, e.Name, e.CreatedDate, e.ResultPublished })
                .Take(5).ToListAsync();
            var events = await (from schedule in _db.ExamSchedules.AsNoTracking()
                join exam in _db.Exams on schedule.ExamId equals exam.Id
                join subject in _db.Subjects on schedule.SubjectId equals subject.Id
                join classroom in _db.Classes on schedule.ClassId equals classroom.Id
                join section in _db.SectionDetails on schedule.SectionId equals section.Id
                where exam.SchoolId == schoolId && schedule.SchoolId == schoolId && schedule.ExamDate >= today
                orderby schedule.ExamDate, schedule.StartTime, schedule.Id
                select new { schedule.Id, examName = exam.Name, subjectName = subject.SubjectName,
                    className = classroom.ClassName, sectionName = section.SectionName,
                    schedule.ExamDate, schedule.StartTime }).Take(5).ToListAsync();
            return Ok(new { success = true, data = new { notices, events } });
        }
        [HttpGet("fee-chart")]
        public async Task<IActionResult> FeeChart([FromQuery] int schoolId, [FromQuery] int year)
        {
            if (schoolId <= 0 || year < 2000 || year > 2100)
                return BadRequest(new { message = "Choose a valid school and year." });
            var roleId = User.FindFirstValue("RoleId");
            if (roleId != "1" && User.FindFirstValue("SchoolId") != schoolId.ToString())
                return Forbid();

            var regularFees = await _db.StudentFees.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsActive)
                .Select(x => new { x.Id, x.Amount, x.Created_Date }).ToListAsync();
            var feeIds = regularFees.Select(x => x.Id).ToList();
            var regularPayments = await _db.FeePayments.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.IsActive && feeIds.Contains(x.StudentFeeId))
                .Select(x => new { x.StudentFeeId, x.AmountPaid, x.Payment_Date }).ToListAsync();
            var transportFees = await _db.TransportFees.AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .Select(x => new { x.Id, x.Amount, x.DueDate }).ToListAsync();
            var transportIds = transportFees.Select(x => x.Id).ToList();
            var transportPayments = await _db.TransportFeePayments.AsNoTracking()
                .Where(x => transportIds.Contains(x.TransportFeeId))
                .Select(x => new { x.TransportFeeId, x.Amount, x.PaymentDate }).ToListAsync();
            var regularPaymentsByFee = regularPayments.ToLookup(x => x.StudentFeeId);
            var transportPaymentsByFee = transportPayments.ToLookup(x => x.TransportFeeId);

            var rows = Enumerable.Range(1, 12).Select(month =>
            {
                var monthEnd = new DateTime(year, month, 1).AddMonths(1);
                var collected = regularPayments.Where(x => x.Payment_Date.Year == year && x.Payment_Date.Month == month)
                    .Sum(x => x.AmountPaid)
                    + transportPayments.Where(x => x.PaymentDate.Year == year && x.PaymentDate.Month == month)
                        .Sum(x => x.Amount);
                var regularDue = regularFees.Where(x => !x.Created_Date.HasValue || x.Created_Date.Value < monthEnd)
                    .Sum(x => Math.Max(0, x.Amount - regularPaymentsByFee[x.Id]
                        .Where(p => p.Payment_Date < monthEnd).Sum(p => p.AmountPaid)));
                var transportDue = transportFees.Where(x => x.DueDate < monthEnd)
                    .Sum(x => Math.Max(0, x.Amount - transportPaymentsByFee[x.Id]
                        .Where(p => p.PaymentDate < monthEnd).Sum(p => p.Amount)));
                return new { month, collected, pending = regularDue + transportDue };
            }).ToList();
            return Ok(new { success = true, data = rows });
        }
        [HttpGet("School-by-superadmin")]
        public async Task<IActionResult> GetSchoolsBySuperAdmin()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetSchoolsBySuperAdminIdAsync(superAdminId);
            return Ok(result);
        }

        [HttpGet("Staff-by-school")]
        public async Task<IActionResult> GetStaffFull(
            [FromQuery] int schoolId,
            int page = 1,
            int pageSize = 10,
            int? staffId = null,
            string? search = null
        )
        {
            try
            {
                var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                if (page == -1)
                {
                    var (_, tempTotal) = await _repo.GetStaffFullAsync(
                        schoolId,
                        1,
                        pageSize,
                        staffId,
                        search
                    );
                    page = Math.Max(1, (int)Math.Ceiling((double)tempTotal / pageSize));
                }

                var (data, total) = await _repo.GetStaffFullAsync(
                    schoolId,
                    page,
                    pageSize,
                    staffId,
                    search
                );
                var totalPages = (int)Math.Ceiling((double)total / pageSize);

                return Ok(
                    new PagedResponse<List<StaffListDto>>
                    {
                        Success = true,
                        Message = "Staff fetched successfully",
                        Data = data,
                        CurrentPage = page,
                        TotalPages = totalPages,
                        TotalRecords = total,
                        PageSize = pageSize,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Success = false, Message = $"Unable to load staff: {ex.Message}" }
                );
            }
        }

        [HttpPut("update-school")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateSchool([FromForm] SchoolUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.UpdateSchoolAsync(dto, userId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut("schools/{schoolId:int}/email-verification")]
        public async Task<IActionResult> SetEmailVerification(int schoolId, [FromBody] EmailVerificationSettingInput input)
        {
            var school = await _db.Schools.FirstOrDefaultAsync(x => x.Id == schoolId && x.IsActive);
            if (school == null) return NotFound(new { message = "School not found." });
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var ownsSchool = school.SuperAdminId == userId || await _db.Staff.AnyAsync(x =>
                x.SchoolId == schoolId && x.usersid == userId && x.IsActive &&
                _db.Roles.Any(r => r.Id == x.RoleId && r.RoleName == "Admin"));
            if (!ownsSchool) return Forbid();
            school.EmailVerificationRequired = input.Required;
            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { school.Id, school.EmailVerificationRequired } });
        }
        [HttpGet("staff-emails")]
        public async Task<IActionResult> GetStaffEmails([FromQuery] int schoolId)
        {
            try
            {
                var emails = await _repo.GetStaffEmailsAsync(schoolId);
                return Ok(new { Success = true, Data = emails });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        Success = false,
                        Message = $"Unable to load existing staff emails: {ex.Message}",
                    }
                );
            }
        }

        [HttpPost("add-staff")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddStaff([FromForm] AddStaffDto dto)
        {
            try
            {
                var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _repo.AddStaffAsync(dto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Success = false, Message = $"Unable to add staff: {ex.Message}" }
                );
            }
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateStaff([FromForm] UpdateStaffDto dto)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.UpdateStaffAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete-document")]
        public async Task<IActionResult> DeleteDocument([FromQuery] int id)
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.DeleteDocumentAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("Get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var superAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _repo.GetRolesBySchoolIdAsync();
            return Ok(result);
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
        {
            var result = await _repo.CreateAcademicSessionAsync(dto);
            return Ok(result);
        }

        [HttpPut("update-parent")]
        public async Task<IActionResult> UpdateParent(UpdateParentDto dto)
        {
            if (
                User.FindFirstValue("RoleId") != "1"
                && (
                    !int.TryParse(User.FindFirstValue("SchoolId"), out var schoolId)
                    || schoolId != dto.SchoolId
                )
            )
                return Forbid();
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();
            var result = await _repo.UpdateParentAsync(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("parents-by-school")]
        public async Task<IActionResult> GetParentsBySchool(
            [FromQuery] int schoolId,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            int? parentId = null
        )
        {
            if (schoolId <= 0)
                return BadRequest("A valid schoolId is required.");

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (data, total) = await _repo.GetParentsBySchoolAsync(
                schoolId,
                page,
                pageSize,
                search,
                parentId
            );

            return Ok(
                new PagedResponse<List<ParentListDto>>
                {
                    Success = true,
                    Message = "Parents fetched successfully",
                    Data = data,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize),
                    TotalRecords = total,
                    PageSize = pageSize,
                }
            );
        }

        [HttpGet("academic-sessions")]
        public async Task<IActionResult> GetAcademicSessions([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
            {
                return BadRequest(
                    new ApiResponse<List<AcademicSessionDto>>
                    {
                        Success = false,
                        Message = "A valid schoolId is required",
                        Data = new List<AcademicSessionDto>(),
                    }
                );
            }

            var result = await _repo.GetAcademicSessionsAsync(schoolId);
            return Ok(result);
        }

        [HttpPut("academic-session-status")]
        public async Task<IActionResult> UpdateAcademicSessionStatus(
            [FromBody] UpdateAcademicSessionStatusDto dto
        )
        {
            if (dto.SchoolId <= 0 || dto.SessionId <= 0)
                return BadRequest(
                    new ApiResponse<string>
                    {
                        Success = false,
                        Message = "A valid school and session are required",
                    }
                );

            var result = await _repo.UpdateAcademicSessionStatusAsync(dto);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("GetStaffAttendanceBySchool")]
        public async Task<IActionResult> GetAttendanceBySchool(int schoolId)
        {
            var result = await _repo.GetStaffAttendanceBySchoolAsync(schoolId);

            if (result == null || !result.Any())
            {
                return NotFound("No attendance records found.");
            }

            return Ok(result);
        }

        [HttpGet("GetStaffAttendanceHistoryByDate")]
        public async Task<IActionResult> GetAttendanceHistory(
            int schoolId,
            DateTime fromDate,
            DateTime toDate,
            int? staffId = null
        )
        {
            if (fromDate > toDate)
            {
                return BadRequest("From date cannot be greater than To date.");
            }

            var result = await _repo.GetAttendanceHistoryAsync(schoolId, fromDate, toDate, staffId);

            if (!result.Any())
            {
                return NotFound("No attendance records found.");
            }

            return Ok(result);
        }
    }
}

public sealed class EmailVerificationSettingInput { public bool Required { get; set; } }
