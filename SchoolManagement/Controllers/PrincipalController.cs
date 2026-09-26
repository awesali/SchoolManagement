using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/principal")]
public class PrincipalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;
    public PrincipalController(AppDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    private async Task<int?> PrincipalSchoolId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user?.School_Id == null) return null;
        var role = await _db.Roles.AsNoTracking().Where(x => x.Id == user.RoleId).Select(x => x.RoleName).FirstOrDefaultAsync();
        return string.Equals(role, "Principal", StringComparison.OrdinalIgnoreCase) ? user.School_Id : null;
    }

    [HttpGet("invigilation")]
    public async Task<IActionResult> Invigilation()
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        var schedules = await (from schedule in _db.ExamSchedules.AsNoTracking()
            join exam in _db.Exams.AsNoTracking() on schedule.ExamId equals exam.Id
            join section in _db.SectionDetails.AsNoTracking() on schedule.SectionId equals section.Id
            join classroom in _db.Classes.AsNoTracking() on schedule.ClassId equals classroom.Id
            join subject in _db.Subjects.AsNoTracking() on schedule.SubjectId equals (int?)subject.Id into subjects
            from subject in subjects.DefaultIfEmpty()
            where schedule.SchoolId == schoolId && schedule.IsActive
            orderby schedule.ExamDate descending
            select new { schedule.Id, schedule.ExamDate, schedule.StartTime, schedule.EndTime,
                examName = exam.Name, className = classroom.ClassName, sectionName = section.SectionName,
                subjectName = subject == null ? "" : subject.SubjectName }).Take(500).ToListAsync();
        var ids = schedules.Select(x => x.Id).ToList();
        var assignments = await (from duty in _db.ExamInvigilators.AsNoTracking()
            join staff in _db.Staff.AsNoTracking() on duty.StaffId equals staff.Id
            where ids.Contains(duty.ExamScheduleId) && staff.SchoolId == schoolId
            select new { duty.Id, duty.ExamScheduleId, duty.StaffId, staffName = staff.Name, duty.DutyType }).ToListAsync();
        var teachers = await _db.Staff.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive && x.RoleId == 2)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync();
        return Ok(new { success = true, data = new { schedules, assignments, teachers } });
    }

    [HttpPost("invigilation")]
    public async Task<IActionResult> AssignInvigilation(InvigilationRequest request)
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        if (request.DutyType is not ("Main" or "Assistant")) return BadRequest(new { message = "Choose Main or Assistant duty." });
        var schedule = await _db.ExamSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ScheduleId && x.SchoolId == schoolId && x.IsActive);
        if (schedule == null || !await _db.Staff.AnyAsync(x => x.Id == request.StaffId && x.SchoolId == schoolId && x.IsActive && x.RoleId == 2))
            return BadRequest(new { message = "Choose a scheduled exam and an active teacher from this school." });
        if (await _db.ExamInvigilators.AnyAsync(x => x.ExamScheduleId == schedule.Id && x.StaffId == request.StaffId))
            return Conflict(new { message = "This teacher is already assigned to this exam." });
        var conflict = await (from duty in _db.ExamInvigilators
            join other in _db.ExamSchedules on duty.ExamScheduleId equals other.Id
            where duty.StaffId == request.StaffId && other.SchoolId == schoolId && other.IsActive &&
                other.ExamDate.Date == schedule.ExamDate.Date && other.StartTime < schedule.EndTime && other.EndTime > schedule.StartTime
            select duty.Id).AnyAsync();
        if (conflict) return Conflict(new { message = "This teacher already has invigilation duty at this time." });
        var item = new ExamInvigilators { ExamScheduleId = schedule.Id, StaffId = request.StaffId, DutyType = request.DutyType };
        _db.ExamInvigilators.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Invigilator assigned." });
    }

    [HttpDelete("invigilation/{id:int}")]
    public async Task<IActionResult> RemoveInvigilation(int id)
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        var assignment = await (from duty in _db.ExamInvigilators
            join schedule in _db.ExamSchedules on duty.ExamScheduleId equals schedule.Id
            where duty.Id == id && schedule.SchoolId == schoolId select duty).FirstOrDefaultAsync();
        if (assignment == null) return NotFound();
        _db.ExamInvigilators.Remove(assignment);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPut("leave/{id:int}/decision")]
    public async Task<IActionResult> DecideLeave(int id, LeaveDecision request)
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        if (request.Status is not ("Approved" or "Rejected")) return BadRequest(new { message = "Choose Approved or Rejected." });
        var leave = await _db.StaffLeaveRequests.FirstOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId && x.IsActive);
        if (leave == null) return NotFound();
        if (leave.Status != "Pending") return Conflict(new { message = "Only pending requests can be decided." });
        leave.Status = request.Status; leave.AdminRemarks = request.Remarks?.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Leave request " + request.Status.ToLowerInvariant() + "." });
    }

    [HttpGet("leave/history")]
    public async Task<IActionResult> LeaveHistory(int page = 1, int pageSize = 20, string? status = null, string? search = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        if (page < 1 || pageSize < 1 || pageSize > 100) return BadRequest(new { message = "Choose a valid page and page size." });
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Decided" or "Pending" or "Approved" or "Rejected"))
            return BadRequest(new { message = "Choose a valid leave status." });
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            return BadRequest(new { message = "The From date must be on or before the To date." });
        var query = from leave in _db.StaffLeaveRequests.AsNoTracking()
            join staff in _db.Staff.AsNoTracking() on leave.StaffId equals staff.Id
            where leave.SchoolId == schoolId && staff.SchoolId == schoolId && leave.IsActive
            select new { leave, staff.Name };
        if (status == "Decided") query = query.Where(x => x.leave.Status == "Approved" || x.leave.Status == "Rejected");
        else if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.leave.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }
        if (fromDate.HasValue) query = query.Where(x => x.leave.ToDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.leave.FromDate.Date <= toDate.Value.Date);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.leave.CreatedDate).ThenByDescending(x => x.leave.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.leave.Id, staffName = x.Name, x.leave.LeaveType, x.leave.FromDate,
                x.leave.ToDate, x.leave.Reason, x.leave.Status, x.leave.AdminRemarks, x.leave.CreatedDate })
            .ToListAsync();
        return Ok(new { success = true, data = rows, total, page, pageSize });
    }

    [HttpGet("leave/upcoming")]
    public async Task<IActionResult> UpcomingApprovedLeave()
    {
        var schoolId = await PrincipalSchoolId();
        if (schoolId == null) return Forbid();
        var start = DateTime.UtcNow.AddHours(5.5).Date;
        var end = start.AddDays(9);
        var rows = await (from leave in _db.StaffLeaveRequests.AsNoTracking()
            join staff in _db.Staff.AsNoTracking() on leave.StaffId equals staff.Id
            where leave.SchoolId == schoolId && staff.SchoolId == schoolId && leave.IsActive &&
                leave.Status == "Approved" && leave.FromDate.Date <= end && leave.ToDate.Date >= start
            orderby leave.FromDate, staff.Name
            select new { leave.Id, staffName = staff.Name, leave.LeaveType, leave.FromDate, leave.ToDate }).ToListAsync();
        return Ok(new { success = true, startDate = start, days = 10, data = rows });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(DateTime? date = null)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null) return Unauthorized();
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.RoleId && x.IsActive);
        // Role and school come from persisted identity, never a client-supplied school.
        if (!string.Equals(role?.RoleName?.Trim(), "Principal", StringComparison.OrdinalIgnoreCase)) return Forbid();
        var schoolId = user.School_Id ?? await _db.Staff.Where(x => x.usersid == userId && x.IsActive)
            .Select(x => (int?)x.SchoolId).FirstOrDefaultAsync();
        var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(x => x.Id == schoolId && x.IsActive);
        if (school == null) return NotFound(new { message = "No active school is assigned to this account." });
        var day = (date ?? DateTime.UtcNow.AddHours(5.5)).Date;
        var end = day.AddDays(1);
        var session = await _db.AcademicSessions.AsNoTracking()
            .Where(x => x.SchoolId == school.Id && x.IsActive && x.Year_Start <= day && x.Year_End >= day)
            .OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
        var sessionId = session?.Id ?? 0;
        var enrollmentQuery = from e in _db.StudentEnrollment.AsNoTracking()
                              join s in _db.Students.AsNoTracking() on e.StudentId equals s.Id
                              where e.SchoolId == school.Id && s.SchoolId == school.Id && e.IsActive && s.IsActive
                                && e.EnrollmentStatus == "Active" && e.SessionId == sessionId
                                && e.EnrollmentDate < end
                              select e;
        var enrollments = session == null ? new List<StudentEnrollment>() : await enrollmentQuery.ToListAsync();
        var ids = enrollments.Select(x => x.Id).ToList();
        var sections = await (from s in _db.SectionDetails.AsNoTracking()
                              join c in _db.Classes.AsNoTracking() on s.ClassId equals c.Id
                              where s.SchoolId == school.Id && c.SchoolId == school.Id && s.IsActive && c.IsActive
                              select new { s.Id, name = c.ClassName + " " + s.SectionName }).ToListAsync();
        async Task<bool> Can(string page) => await _permissions.HasPermissionAsync(User, page + ".read");
        object? students = null, staff = null, academics = null, finance = null, examinations = null;
        if (await Can("attendance.students"))
        {
            var records = await _db.StudentAttendance.AsNoTracking()
                .Where(x => x.School_Id == school.Id && x.IsActive && ids.Contains(x.EnrollmentId)
                    && x.Attendance_Date >= day && x.Attendance_Date < end)
                .ToListAsync();
            var latest = records.GroupBy(x => x.EnrollmentId).Select(g => g.OrderByDescending(x => x.Id).First()).ToList();
            var rows = sections.Select(s => {
                var enrolled = enrollments.Where(x => x.SectionId == s.Id).Select(x => x.Id).ToHashSet();
                var marked = latest.Where(x => enrolled.Contains(x.EnrollmentId)).ToList();
                return new { s.Id, s.name, total = enrolled.Count, recorded = marked.Count,
                    present = marked.Count(x => x.Status == "Present" || x.Status == "Late"),
                    absent = marked.Count(x => x.Status == "Absent"), late = marked.Count(x => x.Status == "Late"),
                    pending = enrolled.Count - marked.Count };
            }).Where(x => x.total > 0).OrderBy(x => x.name).ToList();
            students = new {
                total = enrollments.Count, present = latest.Count(x => x.Status == "Present" || x.Status == "Late"),
                absent = latest.Count(x => x.Status == "Absent"), late = latest.Count(x => x.Status == "Late"),
                recorded = latest.Count, unmarked = enrollments.Count - latest.Count,
                pendingClasses = rows.Count(x => x.pending > 0), classes = rows
            };
        }
        if (await Can("attendance.staff"))
        {
            var employees = await _db.Staff.AsNoTracking().Where(x => x.SchoolId == school.Id && x.IsActive && x.DOJ < end)
                .Select(x => new { x.Id, x.Name }).ToListAsync();
            var employeeIds = employees.Select(x => x.Id).ToList();
            var records = await _db.StaffAttendance.AsNoTracking()
                .Where(x => x.School_Id == school.Id && x.IsActive && employeeIds.Contains(x.Staff_Id)
                    && x.Attendance_Date >= day && x.Attendance_Date < end).ToListAsync();
            var latest = records.GroupBy(x => x.Staff_Id).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().Status);
            var leaves = await _db.StaffLeaveRequests.AsNoTracking()
                .Where(x => x.SchoolId == school.Id && x.IsActive && employeeIds.Contains(x.StaffId)
                    && x.Status == "Approved" && x.FromDate < end && x.ToDate >= day).Select(x => x.StaffId).ToListAsync();
            var people = employees.Select(x => new { x.Id, x.Name,
                status = latest.TryGetValue(x.Id, out var status) ? status : leaves.Contains(x.Id) ? "On Leave" : "Not marked" }).ToList();
            staff = new { total = people.Count, present = people.Count(x => x.status == "Present" || x.status == "Late"),
                absent = people.Count(x => x.status == "Absent"), late = people.Count(x => x.status == "Late"),
                onLeave = people.Count(x => x.status == "On Leave" || x.status == "Leave"),
                unmarked = people.Count(x => x.status == "Not marked"), people };
        }
        object? leaveRequests = null;
        if (await Can("management.staff"))
            leaveRequests = await (from l in _db.StaffLeaveRequests.AsNoTracking()
                                   join s in _db.Staff.AsNoTracking() on l.StaffId equals s.Id
                                   where l.SchoolId == school.Id && s.SchoolId == school.Id && l.IsActive && s.IsActive && l.Status == "Pending"
                                   orderby l.CreatedDate
                                   select new { l.Id, staffName = s.Name, l.LeaveType, l.FromDate, l.ToDate, l.Reason, l.CreatedDate }).ToListAsync();
        if (await Can("academics.classes"))
        {
            var sectionIds = sections.Select(x => x.Id).ToList();
            var weekday = (int)day.DayOfWeek;
            var scheduled = await (from t in _db.Timetables.AsNoTracking()
                                   join p in _db.TimetablePeriods.AsNoTracking() on t.PeriodId equals p.Id
                                   where t.SchoolId == school.Id && t.IsActive && t.DayOfWeek == weekday && !p.IsBreak
                                       && p.SectionId == t.SectionId && sectionIds.Contains(t.SectionId) && t.SubjectId != null
                                   select new { t.Id, t.SectionId }).ToListAsync();
            var homework = await _db.HomeworkAssignments.AsNoTracking()
                .Where(x => x.SchoolId == school.Id && x.IsActive && x.Status == "Published"
                    && sectionIds.Contains(x.SectionId) && x.AssignedDate >= day && x.AssignedDate < end)
                .Select(x => new { x.Id, x.SectionId }).ToListAsync();
            var classActivity = sections.OrderBy(x => x.name).Select(s => new {
                s.Id, s.name,
                scheduledPeriods = scheduled.Count(x => x.SectionId == s.Id),
                homeworkPosted = homework.Count(x => x.SectionId == s.Id)
            }).ToList();
            academics = new { classes = sections.Count, scheduledPeriods = scheduled.Count, homeworkPosted = homework.Count, classActivity };
        }
        if (await Can("exams.academic-exam"))
            examinations = await _db.Exams.AsNoTracking()
                .Where(x => x.SchoolId == school.Id && x.IsActive && x.AcademicSessionId == sessionId)
                .OrderByDescending(x => x.StartDate)
                .Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate, x.ResultPublished, x.IsPublished }).ToListAsync();
        if (await Can("finance.fees"))
        {
            var fees = await _db.StudentFees.AsNoTracking()
                .Where(x => x.SchoolId == school.Id && x.IsActive && x.SessionId == sessionId)
                .Select(x => new { x.Amount, paid = x.FeePayments.Where(p => p.IsActive && p.SchoolId == school.Id && p.Payment_Date < end).Sum(p => (decimal?)p.AmountPaid) ?? 0m })
                .ToListAsync();
            var payments = _db.FeePayments.AsNoTracking().Where(x => x.SchoolId == school.Id && x.IsActive && x.StudentFee.IsActive);
            var month = new DateTime(day.Year, day.Month, 1);
            finance = new {
                today = await payments.Where(x => x.Payment_Date >= day && x.Payment_Date < end).SumAsync(x => (decimal?)x.AmountPaid) ?? 0m,
                month = await payments.Where(x => x.Payment_Date >= month && x.Payment_Date < end).SumAsync(x => (decimal?)x.AmountPaid) ?? 0m,
                outstanding = fees.Sum(x => Math.Max(0, x.Amount - x.paid)),
                assessed = fees.Sum(x => x.Amount), collected = fees.Sum(x => x.paid)
            };
        }
        return Ok(new {
            schoolId = school.Id, schoolName = school.SchoolName, date = day.ToString("yyyy-MM-dd"),
            academicYear = session == null ? null : $"{session.Year_Start:yyyy}–{session.Year_End:yy}",
            generatedAt = DateTime.UtcNow, students, staff, academics, leaveRequests, finance, examinations
        });
    }
}

public record InvigilationRequest(int ScheduleId, int StaffId, string DutyType);
public record LeaveDecision(string Status, string? Remarks);
