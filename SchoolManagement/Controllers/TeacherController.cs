using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/Teacher")]
public class TeacherController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPermissionService _permissions;
    public TeacherController(AppDbContext db, IPermissionService permissions) { _db = db; _permissions = permissions; }

    private async Task<Staff?> CurrentStaff()
    {
        if (User.FindFirstValue("RoleId") != "2") return null;
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _db.Staff.FirstOrDefaultAsync(s => s.usersid == userId && s.IsActive);
    }

    private async Task<List<int>> ClassTeacherSectionIds(Staff staff)
    {
        return await _db.SectionDetails
            .Where(s => s.StaffId == staff.Id && s.SchoolId == staff.SchoolId && s.IsActive)
            .Select(s => s.Id).ToListAsync();
    }

    [HttpGet("workspace")]
    [HttpGet("classes")]
    [HttpGet("timetable")]
    public async Task<IActionResult> Workspace(DateTime? date)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var mappings = await _db.SectionSubjectTeachers
            .Where(m => m.StaffId == staff.Id && m.SchoolId == staff.SchoolId && m.IsActive).ToListAsync();
        var subjectSections = mappings.Select(m => m.SectionId).Distinct().ToList();
        var sections = await _db.SectionDetails.Where(s => s.SchoolId == staff.SchoolId && s.IsActive &&
            (s.StaffId == staff.Id || subjectSections.Contains(s.Id))).ToListAsync();
        var sectionIds = sections.Select(s => s.Id).ToList();
        var classIds = sections.Select(s => s.ClassId).Distinct().ToList();
        var classes = await _db.Classes.Where(c => classIds.Contains(c.Id) && c.SchoolId == staff.SchoolId && c.IsActive).ToListAsync();
        var subjectIds = mappings.Select(m => m.SubjectId).Distinct().ToList();
        var subjects = await _db.Subjects.Where(s => subjectIds.Contains(s.Id)).ToListAsync();
        var periods = await _db.TimetablePeriods.Where(p => sectionIds.Contains(p.SectionId) && !p.IsBreak).ToListAsync();
        var slots = await _db.Timetables.Where(t => sectionIds.Contains(t.SectionId) && t.SchoolId == staff.SchoolId && t.IsActive).ToListAsync();
        var attendanceAllowed = await _permissions.HasPermissionAsync(User, "attendance.students.read");
        var classTeacherSections = sections.Where(s => s.StaffId == staff.Id).Select(s => s.Id).ToList();
        var today = (date ?? DateTime.Today).Date;
        var attendance = attendanceAllowed ? await (from a in _db.StudentAttendance
            join e in _db.StudentEnrollment on a.EnrollmentId equals e.Id
            where classTeacherSections.Contains(e.SectionId) && a.School_Id == staff.SchoolId &&
                a.IsActive && a.Attendance_Date.Date == today
            select new { e.SectionId, a.Status }).ToListAsync() : null;
        var populatedSections = await (from e in _db.StudentEnrollment
            where classTeacherSections.Contains(e.SectionId) && e.IsActive && e.SchoolId == staff.SchoolId
            select e.SectionId).Distinct().ToListAsync();
        return Ok(new {
            success = true,
            attendancePending = attendance == null ? (int?)null : populatedSections.Count(id => !attendance.Any(a => a.SectionId == id)),
            studentsAbsent = attendance == null ? (int?)null : attendance.Count(a => a.Status == "Absent"),
            classes = classes.Select(c => new {
                id = c.Id, className = c.ClassName,
                sections = sections.Where(s => s.ClassId == c.Id).Select(s => new {
                    id = s.Id, sectionName = s.SectionName, isClassTeacher = s.StaffId == staff.Id,
                    subjects = mappings.Where(m => m.SectionId == s.Id).Select(m => new {
                        subjectId = m.SubjectId, subjectName = subjects.FirstOrDefault(v => v.Id == m.SubjectId)?.SubjectName ?? ""
                    })
                })
            }),
            slots = slots.Where(t => t.SubjectId.HasValue &&
                    mappings.Any(m => m.SectionId == t.SectionId && m.SubjectId == t.SubjectId))
                .Where(t => periods.Any(p => p.SectionId == t.SectionId && p.PeriodNumber == t.PeriodId))
                .Select(t => {
                    // Timetable slots store the period number; period rows have their own database IDs.
                    var period = periods.First(p => p.SectionId == t.SectionId && p.PeriodNumber == t.PeriodId);
                    var section = sections.First(s => s.Id == t.SectionId);
                    return new {
                        id = $"{t.SectionId}-{t.DayOfWeek}-{t.PeriodId}",
                        day = t.DayOfWeek, period = period.PeriodNumber, sectionId = section.Id,
                        attendanceStatus = attendance == null || section.StaffId != staff.Id ? null : attendance.Any(a => a.SectionId == section.Id) ? "Submitted" : "Pending",
                        start = period.StartTime.ToString().Substring(0, 5),
                        end = period.EndTime.ToString().Substring(0, 5),
                        className = (classes.FirstOrDefault(c => c.Id == section.ClassId)?.ClassName ?? "") + " · " + section.SectionName,
                        subject = subjects.FirstOrDefault(s => s.Id == t.SubjectId)?.SubjectName ?? ""
                    };
                })
        });
    }

    // Returns assigned sections (for dropdown) + all students across all sections
    [HttpGet("student-attendance-roster")]
    public async Task<IActionResult> Roster()
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var sectionIds = await ClassTeacherSectionIds(staff);
        var assignedSections = await (from section in _db.SectionDetails
            join classroom in _db.Classes on section.ClassId equals classroom.Id
            where sectionIds.Contains(section.Id)
                && section.SchoolId == staff.SchoolId && section.IsActive
                && classroom.SchoolId == staff.SchoolId && classroom.IsActive
            orderby classroom.ClassName, section.SectionName
            select new { sectionId = section.Id, sectionName = section.SectionName, className = classroom.ClassName })
            .ToListAsync();
        return Ok(new { success = true, sections = assignedSections });
    }

    // Returns students enrolled in a specific section for the active session
    [HttpGet("section-students")]
    public async Task<IActionResult> SectionStudents(int sectionId)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var sectionIds = await ClassTeacherSectionIds(staff);
        if (!sectionIds.Contains(sectionId))
            return Forbid();
        var rows = await (from enrollment in _db.StudentEnrollment
            join section in _db.SectionDetails on enrollment.SectionId equals section.Id
            join student in _db.Students on enrollment.StudentId equals student.Id
            join classroom in _db.Classes on enrollment.ClassId equals classroom.Id
            where enrollment.SectionId == sectionId
                && enrollment.SchoolId == staff.SchoolId && enrollment.IsActive
                && student.SchoolId == staff.SchoolId && student.IsActive
                && section.SchoolId == staff.SchoolId && section.IsActive
            orderby enrollment.RollNumber, student.StudentName
            select new {
                id = student.Id,
                enrollmentId = enrollment.Id,
                studentName = student.StudentName,
                rollNumber = enrollment.RollNumber,
                className = classroom.ClassName,
                sectionName = section.SectionName,
                sectionId = section.Id
            }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("student-attendance-history")]
    public async Task<IActionResult> AttendanceHistory(int sectionId, DateTime date)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var sectionIds = await ClassTeacherSectionIds(staff);
        if (!sectionIds.Contains(sectionId))
            return Forbid();
        var rows = await (from attendance in _db.StudentAttendance
            join enrollment in _db.StudentEnrollment on attendance.EnrollmentId equals enrollment.Id
            join section in _db.SectionDetails on enrollment.SectionId equals section.Id
            join student in _db.Students on attendance.Student_Id equals student.Id
            join classroom in _db.Classes on enrollment.ClassId equals classroom.Id
            where enrollment.SectionId == sectionId
                && attendance.School_Id == staff.SchoolId
                && attendance.IsActive && attendance.Attendance_Date.Date == date.Date
            select new {
                studentId = student.Id,
                studentName = student.StudentName,
                sectionId = section.Id,
                sectionName = section.SectionName,
                className = classroom.ClassName,
                enrollmentId = enrollment.Id,
                status = attendance.Status
            }).ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("student-attendance")]
    public async Task<IActionResult> SaveAttendance(MarkBulkAttendanceDto dto)
    {
        var staff = await CurrentStaff();
        if (staff == null) return Forbid();
        var sectionIds = await ClassTeacherSectionIds(staff);
        var section = await _db.SectionDetails.FirstOrDefaultAsync(s =>
            s.Id == dto.SectionId && s.SchoolId == staff.SchoolId && s.IsActive
            && sectionIds.Contains(s.Id));
        if (section == null) return Forbid();
        if (dto.AttendanceDate.Date < DateTime.UtcNow.Date.AddDays(-1) ||
            dto.AttendanceDate.Date > DateTime.UtcNow.Date.AddDays(1))
            return BadRequest(new { success = false, message = "Attendance can only be submitted for the current school day." });
        var validStatuses = new[] { "Present", "Absent", "Late", "Half Day", "Leave", "Excused" };
        if (dto.Students == null || dto.Students.Count == 0 || dto.Students.Any(s => !validStatuses.Contains(s.Status)))
            return BadRequest(new { success = false, message = "Every student requires a valid attendance status." });
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var enrollments = await (from e in _db.StudentEnrollment
            join student in _db.Students on e.StudentId equals student.Id
            where e.SectionId == dto.SectionId && e.SchoolId == staff.SchoolId
                && e.IsActive
                && student.IsActive && student.SchoolId == staff.SchoolId
            select e).ToListAsync();
        if (dto.Students.Count != enrollments.Count ||
            dto.Students.Select(s => s.EnrollmentId).Distinct().Count() != enrollments.Count ||
            dto.Students.Any(s => !enrollments.Any(e => e.Id == s.EnrollmentId && e.StudentId == s.StudentId)))
            return BadRequest(new { success = false, message = "The class roster changed. Reload and mark the complete class." });
        var ids = enrollments.Select(e => e.Id).ToList();
        if (await _db.StudentAttendance.AnyAsync(a => ids.Contains(a.EnrollmentId) &&
            a.School_Id == staff.SchoolId && a.Attendance_Date.Date == dto.AttendanceDate.Date && a.IsActive))
            return Conflict(new { success = false, message = "Attendance is already submitted. Reload to view the saved register." });
        _db.StudentAttendance.AddRange(dto.Students.Select(s => new StudentAttendance {
            Student_Id = s.StudentId, EnrollmentId = s.EnrollmentId, School_Id = staff.SchoolId,
            Attendance_Date = dto.AttendanceDate.Date, Status = s.Status,
            Created_By = staff.usersid, Created_At = DateTime.Now, IsActive = true
        }));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, message = "Class attendance submitted." });
    }
}
