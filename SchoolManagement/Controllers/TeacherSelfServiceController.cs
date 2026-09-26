using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/Teacher")]
public class TeacherSelfServiceController : ControllerBase
{
    private readonly AppDbContext _db;
    public TeacherSelfServiceController(AppDbContext db) => _db = db;

    private async Task<Staff?> CurrentStaff()
    {
        if (User.FindFirstValue("RoleId") != "2" ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.usersid == userId && x.IsActive);
    }

    private async Task<List<SectionSubjectTeachers>> TeachingAssignments(Staff staff) =>
        await _db.SectionSubjectTeachers.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId && x.IsActive)
            .ToListAsync();

    private async Task<bool> CanTeach(Staff staff, int sectionId, int subjectId) =>
        await _db.SectionSubjectTeachers.AnyAsync(x => x.StaffId == staff.Id && x.SchoolId == staff.SchoolId &&
            x.SectionId == sectionId && x.SubjectId == subjectId && x.IsActive);

    [HttpGet("teaching-options")]
    public async Task<IActionResult> TeachingOptions()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var mappings = await TeachingAssignments(staff);
        var sectionIds = mappings.Select(x => x.SectionId).Distinct().ToList();
        var subjectIds = mappings.Select(x => x.SubjectId).Distinct().ToList();
        var sections = await _db.SectionDetails.AsNoTracking().Where(x => sectionIds.Contains(x.Id) && x.IsActive).ToListAsync();
        var classes = await _db.Classes.AsNoTracking().Where(x => sections.Select(s => s.ClassId).Contains(x.Id) && x.IsActive).ToListAsync();
        var subjects = await _db.Subjects.AsNoTracking().Where(x => subjectIds.Contains(x.Id) && x.IsActive).ToListAsync();
        return Ok(new { success = true, data = mappings.Where(mapping => sections.Any(section => section.Id == mapping.SectionId)).Select(mapping => {
            var section = sections.First(x => x.Id == mapping.SectionId);
            return new {
                sectionId = section.Id, sectionName = section.SectionName,
                className = classes.FirstOrDefault(x => x.Id == section.ClassId)?.ClassName ?? "",
                subjectId = mapping.SubjectId,
                subjectName = subjects.FirstOrDefault(x => x.Id == mapping.SubjectId)?.SubjectName ?? ""
            };
        }).OrderBy(x => x.className).ThenBy(x => x.sectionName).ThenBy(x => x.subjectName) });
    }

    [HttpGet("homework")]
    public async Task<IActionResult> Homework()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await (from homework in _db.HomeworkAssignments.AsNoTracking()
            join section in _db.SectionDetails on homework.SectionId equals section.Id
            join classroom in _db.Classes on section.ClassId equals classroom.Id
            join subject in _db.Subjects on homework.SubjectId equals subject.Id
            where homework.StaffId == staff.Id && homework.SchoolId == staff.SchoolId && homework.IsActive
            orderby homework.DueDate descending
            select new { homework.Id, homework.Title, homework.Description, homework.AssignedDate, homework.DueDate,
                homework.TotalMarks, homework.ResourceUrl, homework.Status, homework.SectionId, homework.SubjectId,
                classroom.ClassName, section.SectionName, subject.SubjectName }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("homework")]
    public async Task<IActionResult> CreateHomework(TeacherHomeworkRequest request)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await CanTeach(staff, request.SectionId, request.SubjectId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { success = false, message = "Title and instructions are required." });
        string? normalizedHomeworkUrl = null;
        if (!string.IsNullOrWhiteSpace(request.ResourceUrl))
        {
            if (!Uri.TryCreate(request.ResourceUrl, UriKind.Absolute, out var homeworkResource) ||
                (homeworkResource.Scheme != Uri.UriSchemeHttp && homeworkResource.Scheme != Uri.UriSchemeHttps))
                return BadRequest(new { success = false, message = "Supporting material must use a valid web link." });
            normalizedHomeworkUrl = homeworkResource.ToString();
        }
        var assigned = request.AssignedDate.Date;
        var due = request.DueDate;
        if (due < assigned) return BadRequest(new { success = false, message = "Due date cannot be before the assigned date." });
        var item = new HomeworkAssignment {
            SchoolId = staff.SchoolId, StaffId = staff.Id, SectionId = request.SectionId, SubjectId = request.SubjectId,
            Title = request.Title.Trim(), Description = request.Description.Trim(), AssignedDate = assigned, DueDate = due,
            TotalMarks = request.TotalMarks, ResourceUrl = normalizedHomeworkUrl,
            Status = request.Publish ? "Published" : "Draft"
        };
        _db.HomeworkAssignments.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = request.Publish ? "Homework published." : "Homework saved as draft.", data = new { item.Id } });
    }

    [HttpGet("study-materials")]
    public async Task<IActionResult> StudyMaterials()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await (from material in _db.TeacherStudyMaterials.AsNoTracking()
            join section in _db.SectionDetails on material.SectionId equals section.Id
            join classroom in _db.Classes on section.ClassId equals classroom.Id
            join subject in _db.Subjects on material.SubjectId equals subject.Id
            where material.StaffId == staff.Id && material.SchoolId == staff.SchoolId && material.IsActive
            orderby material.CreatedDate descending
            select new { material.Id, material.Title, material.Description, material.ResourceType, material.ResourceUrl,
                material.CreatedDate, material.SectionId, material.SubjectId, classroom.ClassName, section.SectionName,
                subject.SubjectName }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("study-materials")]
    public async Task<IActionResult> CreateStudyMaterial(TeacherStudyMaterialRequest request)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (!await CanTeach(staff, request.SectionId, request.SubjectId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || !Uri.TryCreate(request.ResourceUrl, UriKind.Absolute, out var resource) ||
            (resource.Scheme != Uri.UriSchemeHttp && resource.Scheme != Uri.UriSchemeHttps))
            return BadRequest(new { success = false, message = "Enter a title and a valid web resource link." });
        if (request.ResourceType is not ("Link" or "PDF" or "Worksheet" or "Notes"))
            return BadRequest(new { success = false, message = "Choose a valid resource type." });
        var item = new TeacherStudyMaterial {
            SchoolId = staff.SchoolId, StaffId = staff.Id, SectionId = request.SectionId, SubjectId = request.SubjectId,
            Title = request.Title.Trim(), Description = request.Description?.Trim(),
            ResourceType = string.IsNullOrWhiteSpace(request.ResourceType) ? "Link" : request.ResourceType.Trim(),
            ResourceUrl = resource.ToString()
        };
        _db.TeacherStudyMaterials.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Study material shared.", data = new { item.Id } });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar([FromQuery(Name = "from")] DateTime rangeStart, [FromQuery(Name = "to")] DateTime rangeEnd)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (rangeStart == default || rangeEnd == default || rangeEnd.Date < rangeStart.Date || (rangeEnd.Date - rangeStart.Date).TotalDays > 62)
            return BadRequest(new { success = false, message = "Choose a valid calendar range of up to 62 days." });
        var mappings = await TeachingAssignments(staff);
        var sectionIds = mappings.Select(x => x.SectionId).Distinct().ToList();
        var assignments = await _db.HomeworkAssignments.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.IsActive && x.DueDate.Date >= rangeStart.Date && x.DueDate.Date <= rangeEnd.Date)
            .Select(x => new TeacherCalendarItem { Date = x.DueDate, Title = x.Title, Type = "Homework", Detail = x.Status }).ToListAsync();
        var examRows = await (from schedule in _db.ExamSchedules.AsNoTracking()
            join exam in _db.Exams on schedule.ExamId equals exam.Id
            join subject in _db.Subjects on schedule.SubjectId equals subject.Id
            where schedule.SchoolId == staff.SchoolId && schedule.IsActive && sectionIds.Contains(schedule.SectionId) &&
                schedule.ExamDate.Date >= rangeStart.Date && schedule.ExamDate.Date <= rangeEnd.Date
            select new { schedule.SectionId, schedule.SubjectId, schedule.ExamDate, exam.Name,
                subject.SubjectName, schedule.StartTime }).ToListAsync();
        var exams = examRows.Where(row => mappings.Any(mapping => mapping.SectionId == row.SectionId && mapping.SubjectId == row.SubjectId))
            .Select(row => new TeacherCalendarItem { Date = row.ExamDate, Title = row.Name + " · " + row.SubjectName,
                Type = "Exam", Detail = row.StartTime.ToString(@"hh\:mm") }).ToList();
        var leave = await _db.StaffLeaveRequests.AsNoTracking()
            .Where(x => x.StaffId == staff.Id && x.IsActive && x.ToDate.Date >= rangeStart.Date && x.FromDate.Date <= rangeEnd.Date)
            .Select(x => new TeacherCalendarItem { Date = x.FromDate, Title = x.LeaveType, Type = "Leave", Detail = x.Status }).ToListAsync();
        return Ok(new { success = true, data = assignments.Concat(exams).Concat(leave).OrderBy(x => x.Date) });
    }

    [HttpGet("profile-summary")]
    public async Task<IActionResult> ProfileSummary()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var role = await _db.Roles.AsNoTracking().Where(x => x.Id == staff.RoleId).Select(x => x.RoleName).FirstOrDefaultAsync();
        var school = await _db.Schools.AsNoTracking().Where(x => x.Id == staff.SchoolId).Select(x => x.SchoolName).FirstOrDefaultAsync();
        return Ok(new { success = true, data = new { staff.Id, staff.Name, staff.Email, staff.Phone, staff.DOB, staff.DOJ,
            staff.GenderCode, staff.EmploymentType, staff.Adress, staff.AddressLine2, staff.Landmark, staff.City,
            staff.District, staff.State, staff.Country, staff.PinCode, staff.Qualification, staff.Specialization,
            staff.ExperienceYears, designation = role, schoolName = school } });
    }

    [HttpGet("leave")]
    public async Task<IActionResult> Leave()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await _db.StaffLeaveRequests.AsNoTracking().Where(x => x.StaffId == staff.Id && x.IsActive)
            .OrderByDescending(x => x.FromDate).Select(x => new { x.Id, x.LeaveType, x.FromDate, x.ToDate,
                x.Reason, x.Status, x.AdminRemarks, x.CreatedDate }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("leave")]
    public async Task<IActionResult> ApplyLeave(TeacherLeaveRequest request)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        if (request.FromDate.Date < DateTime.Today || request.ToDate.Date < request.FromDate.Date || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { success = false, message = "Choose valid future dates and enter a reason." });
        var allowedLeaveTypes = new[] { "Casual Leave", "Sick Leave", "Earned Leave", "Unpaid Leave" };
        if (!allowedLeaveTypes.Contains(request.LeaveType))
            return BadRequest(new { success = false, message = "Choose a valid leave type." });
        if (await _db.StaffLeaveRequests.AnyAsync(x => x.StaffId == staff.Id && x.IsActive && x.Status != "Rejected" &&
            x.FromDate.Date <= request.ToDate.Date && x.ToDate.Date >= request.FromDate.Date))
            return Conflict(new { success = false, message = "A leave request already covers these dates." });
        var item = new StaffLeaveRequest { SchoolId = staff.SchoolId, StaffId = staff.Id,
            LeaveType = request.LeaveType.Trim(), FromDate = request.FromDate.Date, ToDate = request.ToDate.Date,
            Reason = request.Reason.Trim() };
        _db.StaffLeaveRequests.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Leave request submitted.", data = new { item.Id } });
    }

    [HttpGet("payslips")]
    public async Task<IActionResult> Payslips()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await _db.SalaryPayment.AsNoTracking().Where(x => x.StaffId == staff.Id && x.schoolId == staff.SchoolId)
            .OrderByDescending(x => x.SalaryYear).ThenByDescending(x => x.SalaryMonth)
            .Select(x => new { x.Id, x.SalaryMonth, x.SalaryYear, x.BasicSalary, x.Bonus, x.Deduction,
                x.NetSalary, x.Status, x.PaymentDate, x.PaymentMethod, x.Remarks }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var rows = await _db.StaffDocuments.AsNoTracking().Where(x => x.StaffId == staff.Id)
            .OrderByDescending(x => x.CreatedDate).Select(x => new { id = x.Id, x.DocumentName,
                documentUrl = x.FileUrl, x.CreatedDate }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }
}

public class TeacherHomeworkRequest
{
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? TotalMarks { get; set; }
    public string? ResourceUrl { get; set; }
    public bool Publish { get; set; } = true;
}

public class TeacherStudyMaterialRequest
{
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string ResourceType { get; set; } = "Link";
    public string ResourceUrl { get; set; } = "";
}

public class TeacherLeaveRequest
{
    public string LeaveType { get; set; } = "Casual Leave";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Reason { get; set; } = "";
}

public class TeacherCalendarItem
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Detail { get; set; } = "";
}
