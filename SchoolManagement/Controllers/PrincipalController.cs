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
