using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;

namespace SchoolManagement.Repository
{
    public class ExamRepository : IExamRepository
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ExamRepository(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ---------------- CREATE EXAM ----------------
        //public async Task CreateExamSchedulesAsync(CreateExamScheduleRequest request)
        //{
        //    // Ã°Å¸â€Â¥ Step 1: Create Exam (GROUP)
        //    var exam = new Exams
        //    {
        //        Name = request.Name, // e.g. "Nursery Midterm April"
        //        ExamTypeId = request.ExamTypeId,
        //        SchoolId = request.SchoolId,
        //        StartDate = request.StartDate,
        //        EndDate = request.EndDate,
        //        IsPublished = false
        //    };

        //    _context.Exams.Add(exam);
        //    await _context.SaveChangesAsync(); // Ã°Å¸â€˜â€° ExamId mil gaya

        //    var schedules = new List<ExamSchedules>();

        //    // Ã°Å¸â€Â¥ Step 2: Add schedules under SAME ExamId
        //    foreach (var cls in request.Classes)
        //    {
        //        foreach (var sec in cls.Sections)
        //        {
        //            foreach (var sub in sec.Subjects)
        //            {
        //                schedules.Add(new ExamSchedules
        //                {
        //                    ExamId = exam.Id,   // Ã¢Å“â€¦ GROUPING FIX
        //                    ExamTypeId = request.ExamTypeId,
        //                    SchoolId = request.SchoolId,
        //                    ClassId = cls.ClassId,
        //                    SectionId = sec.SectionId,
        //                    SubjectId = sub.SubjectId,
        //                    ExamDate = sub.ExamDate,
        //                    StartTime = sub.StartTime,
        //                    EndTime = sub.EndTime
        //                });
        //            }
        //        }
        //    }

        //    await _context.ExamSchedules.AddRangeAsync(schedules);
        //    await _context.SaveChangesAsync();
        //}

        //// ---------------- ASSIGN INVIGILATOR ----------------
        //public async Task<int> AssignInvigilatorAsync(AssignInvigilatorDto dto)
        //{
        //    var schedule = await _context.ExamSchedules
        //        .FirstOrDefaultAsync(x => x.Id == dto.ExamScheduleId);

        //    if (schedule == null)
        //        throw new Exception("Schedule not found");

        //    // Ã°Å¸â€Â¥ STAFF CONFLICT CHECK
        //    var conflict = await _context.ExamInvigilators
        //        .Include(x => x.ExamSchedule)
        //        .AnyAsync(x =>
        //            x.StaffId == dto.StaffId &&
        //            x.ExamSchedule.ExamDate == schedule.ExamDate &&
        //            (
        //                (schedule.StartTime >= x.ExamSchedule.StartTime && schedule.StartTime < x.ExamSchedule.EndTime) ||
        //                (schedule.EndTime > x.ExamSchedule.StartTime && schedule.EndTime <= x.ExamSchedule.EndTime)
        //            )
        //    );

        //    if (conflict)
        //        throw new Exception("Staff already assigned in another exam");

        //    var inv = new ExamInvigilators
        //    {
        //        ExamScheduleId = dto.ExamScheduleId,
        //        StaffId = dto.StaffId,
        //        DutyType = dto.DutyType
        //    };

        //    _context.ExamInvigilators.Add(inv);
        //    await _context.SaveChangesAsync();

        //    return inv.Id;
        //}


        //// ---------------- EXAM TYPE PICKLIST ----------------
        //public async Task<List<ExamTypePicklistDto>> GetExamTypePicklistAsync(int schoolId)
        //{
        //    return await _context.ExamTypes
        //        .Where(x => x.IsActive && x.schoolId == schoolId)
        //        .Select(x => new ExamTypePicklistDto { Id = x.Id, Name = x.Name })
        //        .ToListAsync();
        //}

        //// ---------------- PUBLISH ----------------
        ////public async Task<bool> PublishExamAsync(int examId)
        ////{
        ////    var exam = await _context.Exams.FindAsync(examId);

        ////    if (exam == null)
        ////        return false;

        ////    exam.IsPublished = true;
        ////    await _context.SaveChangesAsync();

        ////    return true;
        ////}

        //// ---------------- SCHEDULED EXAMS LIST ----------------
        //public async Task<(List<ExamScheduleListDto> Data, int Total)> GetScheduledExamsAsync(int schoolId, int page, int pageSize)
        //{
        //    var query = from e in _context.Exams
        //                join et in _context.ExamTypes on e.ExamTypeId equals et.Id
        //                where e.SchoolId == schoolId
        //                select new ExamScheduleListDto
        //                {
        //                    ExamId = e.Id,
        //                    ExamName = e.Name,
        //                    ExamTitle = et.Name,
        //                    StartDate = e.StartDate,
        //                    EndDate = e.EndDate,

        //                    // Ã°Å¸â€Â¥ Count distinct classes
        //                    ClassCount = _context.ExamSchedules
        //                        .Where(es => es.ExamId == e.Id)
        //                        .Select(es => es.ClassId)
        //                        .Distinct()
        //                        .Count()
        //                };

        //    var total = await query.CountAsync();

        //    var data = await query
        //        .OrderByDescending(x => x.StartDate)
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    return (data, total);
        //}

        //// ---------------- EXAM DETAIL ----------------
        //public async Task<ExamDetailDto?> GetExamDetailAsync(int examId, int schoolId)
        //{
        //    var exam = await _context.Exams
        //        .Where(e => e.Id == examId && e.SchoolId == schoolId)
        //        .Join(_context.ExamTypes, e => e.ExamTypeId, et => et.Id,
        //            (e, et) => new { e, et })
        //        .FirstOrDefaultAsync();

        //    if (exam == null) return null;

        //    var schedules = await (
        //        from es in _context.ExamSchedules
        //        join c in _context.Classes on es.ClassId equals c.Id
        //        join s in _context.SectionDetails on es.SectionId equals s.Id
        //        join sub in _context.Subjects on es.SubjectId equals sub.Id
        //        where es.ExamId == examId && es.SchoolId == schoolId
        //        select new
        //        {
        //            es.ClassId, c.ClassName,
        //            es.SectionId, s.SectionName,
        //            es.SubjectId, sub.SubjectName,
        //            es.ExamDate, es.StartTime, es.EndTime
        //        }
        //    ).ToListAsync();

        //    var classes = schedules
        //        .GroupBy(x => new { x.ClassId, x.ClassName })
        //        .Select(cg => new ExamDetailClassDto
        //        {
        //            ClassId = cg.Key.ClassId,
        //            ClassName = cg.Key.ClassName,
        //            Sections = cg.GroupBy(x => new { x.SectionId, x.SectionName })
        //                .Select(sg => new ExamDetailSectionDto
        //                {
        //                    SectionId = sg.Key.SectionId,
        //                    SectionName = sg.Key.SectionName,
        //                    Subjects = sg.Select(x => new ExamDetailSubjectDto
        //                    {
        //                        SubjectId = x.SubjectId,
        //                        SubjectName = x.SubjectName,
        //                        ExamDate = x.ExamDate,
        //                        StartTime = x.StartTime,
        //                        EndTime = x.EndTime
        //                    }).ToList()
        //                }).ToList()
        //        }).ToList();

        //    return new ExamDetailDto
        //    {
        //        ExamId = exam.e.Id,
        //        ExamName = exam.e.Name,
        //        ExamType = exam.et.Name,
        //        StartDate = exam.e.StartDate,
        //        EndDate = exam.e.EndDate,
        //        Classes = classes
        //    };
        //}

        //public async Task<List<ExamInvigilators>> GetInvigilatorsByScheduleAsync(int scheduleId)
        //{
        //    return await _context.ExamInvigilators
        //        .Include(x => x.ExamSchedule)
        //        .Where(x => x.ExamScheduleId == scheduleId)
        //        .ToListAsync();
        //}

        public async Task<ApiResponse<ExamTypes>>CreateExamType(CreateExamTypeDto dto, int userId)
        {
            try
            {
                var examType = new ExamTypes
                {
                    Name = dto.Name,
                    schoolId = dto.SchoolId,
                    IsActive = true
                };

                _context.ExamTypes.Add(examType);

                await _context.SaveChangesAsync();

                return new ApiResponse<ExamTypes>
                {
                    Success = true,
                    Message = "Exam Type Created Successfully",
                    Data = examType
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ExamTypes>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<List<ExamTypes>>>GetExamTypes(int schoolId)
        {
            try
            {
                var data = await _context.ExamTypes
                    .Where(x =>
                        x.schoolId == schoolId &&
                        x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                return new ApiResponse<List<ExamTypes>>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<ExamTypes>>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<Exams>>CreateExam(CreateExamDto dto, int userId)
        {
            try
            {
                if (dto.EndDate.Date < dto.StartDate.Date)
                {
                    return new ApiResponse<Exams>
                    {
                        Success = false,
                        Message = "Exam end date must be the same as or later than the start date."
                    };
                }

                var exam = new Exams
                {
                    Name = dto.Name,
                    ExamTypeId = dto.ExamTypeId,
                    SchoolId = dto.SchoolId,
                    AcademicSessionId = dto.AcademicSessionId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsPublished = false,
                    ResultPublished = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userId,
                    IsActive = true
                };

                _context.Exams.Add(exam);

                await _context.SaveChangesAsync();

                return new ApiResponse<Exams>
                {
                    Success = true,
                    Message = "Exam Created Successfully",
                    Data = exam
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Exams>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<List<Exams>>>GetExams(int schoolId)
        {
            try
            {
                var exams = await (
                    from e in _context.Exams
                    join et in _context.ExamTypes
                        on e.ExamTypeId equals et.Id
                    where e.SchoolId == schoolId
                    select new Exams
                    {
                        Id = e.Id,
                        Name = e.Name,
                        ExamTypeId = e.ExamTypeId,
                        SchoolId = e.SchoolId,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        IsPublished = e.IsPublished,
                        ResultPublished = e.ResultPublished,
                        CreatedDate = e.CreatedDate,
                        IsActive = e.IsActive
                    })
                    .ToListAsync();

                return new ApiResponse<List<Exams>>
                {
                    Success = true,
                    Data = exams
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Exams>>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<Exams>> CreateTeacherUnitTest(CreateTeacherUnitTestDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return new ApiResponse<Exams> { Success = false, Message = "Unit test name is required" };
            if (dto.MaxMarks <= 0 || dto.PassingMarks < 0 || dto.PassingMarks >= dto.MaxMarks)
                return new ApiResponse<Exams> { Success = false, Message = "Passing marks must be less than total marks" };

            var staff = await _context.Staff
                .Where(s => EF.Property<int?>(s, nameof(Staff.usersid)) == userId)
                .Select(s => new { s.Id, SchoolId = EF.Property<int?>(s, nameof(Staff.SchoolId)) })
                .FirstOrDefaultAsync();
            if (staff == null || !staff.SchoolId.HasValue)
                return new ApiResponse<Exams> { Success = false, Message = "Teacher profile or school not found" };

            var assigned = await _context.SectionDetails.AnyAsync(s => s.Id == dto.SectionId &&
                s.ClassId == dto.ClassId && s.StaffId == staff.Id && s.SchoolId == staff.SchoolId.Value && s.IsActive);
            var subjectAssigned = await _context.SectionSubjects.AnyAsync(s =>
                s.SectionId == dto.SectionId && s.SubjectId == dto.SubjectId && s.IsActive);
            if (!assigned || !subjectAssigned)
                return new ApiResponse<Exams> { Success = false, Message = "You can add a unit test only for your assigned class, section and subject" };

            var unitTestType = await _context.ExamTypes.FirstOrDefaultAsync(x =>
                x.schoolId == staff.SchoolId.Value && x.IsActive && x.Name.ToLower() == "unit test");
            if (unitTestType == null)
            {
                unitTestType = new ExamTypes { Name = "Unit Test", schoolId = staff.SchoolId.Value, IsActive = true };
                _context.ExamTypes.Add(unitTestType);
                await _context.SaveChangesAsync();
            }

            var session = await _context.AcademicSessions
                .Where(x => x.SchoolId == staff.SchoolId.Value && x.IsActive)
                .OrderByDescending(x => x.Year_Start).FirstOrDefaultAsync();
            if (session == null)
                return new ApiResponse<Exams> { Success = false, Message = "No active academic session found" };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var exam = new Exams
            {
                Name = dto.Name.Trim(), ExamTypeId = unitTestType.Id, SchoolId = staff.SchoolId.Value,
                AcademicSessionId = session.Id, StartDate = dto.TestDate.Date, EndDate = dto.TestDate.Date,
                IsPublished = true, ResultPublished = false, CreatedDate = DateTime.Now, CreatedBy = userId, IsActive = true
            };
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            _context.ExamSubjects.Add(new ExamSubjects
            {
                SchoolId = staff.SchoolId.Value, ExamId = exam.Id, ClassId = dto.ClassId,
                SectionId = dto.SectionId, SubjectId = dto.SubjectId, MaxMarks = dto.MaxMarks,
                PassingMarks = dto.PassingMarks, Created_Date = DateTime.Now, IsActive = true
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ApiResponse<Exams> { Success = true, Message = "Unit test created successfully", Data = exam };
        }

        public async Task<ApiResponse<Exams>>PublishExam(int examId)
        {
            try
            {
                var exam = await _context.Exams
                    .FirstOrDefaultAsync(x => x.Id == examId);

                if (exam == null)
                {
                    return new ApiResponse<Exams>
                    {
                        Success = false,
                        Message = "Exam not found"
                    };
                }

                exam.IsPublished = true;
                await _context.SaveChangesAsync();

                await SendExamPublishEmailsAsync(exam);

                return new ApiResponse<Exams>
                {
                    Success = true,
                    Message = "Exam Published Successfully",
                    Data = exam
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Exams>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private async Task SendExamPublishEmailsAsync(Exams exam)
        {
            var classSections = await _context.ExamSubjects
                .Where(x => x.ExamId == exam.Id && x.IsActive)
                .Select(x => new { x.ClassId, x.SectionId, x.SubjectId })
                .Distinct()
                .ToListAsync();

            foreach (var cs in classSections)
            {
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(x => x.Id == cs.SubjectId);

                var students = await (
                    from se in _context.StudentEnrollment
                    join st in _context.Students on se.StudentId equals st.Id
                    where se.ClassId == cs.ClassId
                        && se.SectionId == cs.SectionId
                        && se.SchoolId == exam.SchoolId
                        && se.IsActive
                        && !string.IsNullOrEmpty(st.Email)
                    select new { st.StudentName, st.Email }
                ).ToListAsync();

                foreach (var student in students)
                {
                    try
                    {
                        var (emailSubject, body) = await _emailService.GetEmailTemplateAsync("ExamPublished",
                            new Dictionary<string, string>
                            {
                        { "StudentName", student.StudentName },
                        { "ExamName", exam.Name },
                        { "SubjectName", subject?.SubjectName ?? "" },
                        { "StartDate", exam.StartDate?.ToString("dd MMM yyyy") ?? "" },
                        { "EndDate", exam.EndDate?.ToString("dd MMM yyyy") ?? "" }
                            });

                        await _emailService.SendEmailAsync(
                            student.Email,
                            emailSubject,
                            body);
                    }
                    catch
                    {
                        // Don't fail publish if email fails
                    }
                }
            }
        }

        public async Task<ApiResponse<ExamSubjects>>AddExamSubject(AddExamSubjectDto dto, int userId)
        {
            try
            {
                if (dto.MaxMarks <= 0)
                    return new ApiResponse<ExamSubjects> { Success = false, Message = "Total marks must be greater than zero." };

                if (dto.PassingMarks < 0 || dto.PassingMarks >= dto.MaxMarks)
                    return new ApiResponse<ExamSubjects> { Success = false, Message = "Passing marks must be less than total marks." };

                var exists = await _context.ExamSubjects
                    .AnyAsync(x =>
                        x.ExamId == dto.ExamId &&
                        x.SubjectId == dto.SubjectId &&
                        x.ClassId == dto.ClassId &&
                        x.SectionId == dto.SectionId);

                if (exists)
                {
                    return new ApiResponse<ExamSubjects>
                    {
                        Success = false,
                        Message = "Subject already added"
                    };
                }

                var entity = new ExamSubjects
                {
                    SchoolId = dto.SchoolId,
                    ExamId = dto.ExamId,
                    ClassId = dto.ClassId,
                    SectionId = dto.SectionId,
                    SubjectId = dto.SubjectId,
                    MaxMarks = dto.MaxMarks,
                    PassingMarks = dto.PassingMarks,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };

                _context.ExamSubjects.Add(entity);

                await _context.SaveChangesAsync();

                return new ApiResponse<ExamSubjects>
                {
                    Success = true,
                    Message = "Subject Added",
                    Data = entity
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ExamSubjects>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<List<ExamSubjectResponseDto>>>GetExamSubjects(int examId)
        {
            try
            {
                var data = await (
from es in _context.ExamSubjects

join s in _context.Subjects
    on es.SubjectId equals s.Id

join c in _context.Classes
    on es.ClassId equals c.Id

join sec in _context.SectionDetails
    on es.SectionId equals sec.Id into secJoin
from sec in secJoin.DefaultIfEmpty()

from sch in _context.ExamSchedules
    .Where(x =>
        x.ExamId == es.ExamId &&
        x.ClassId == es.ClassId &&
        x.SubjectId == es.SubjectId &&
        x.SectionId == es.SectionId)
    .DefaultIfEmpty()

where es.ExamId == examId

select new ExamSubjectResponseDto
{
    Id = es.Id,
    SubjectId = es.SubjectId,
    SubjectName = s.SubjectName,

    ClassId = c.Id,
    ClassName = c.ClassName,

    SectionId = sec != null ? sec.Id : (int?)null,
    SectionName = sec != null ? sec.SectionName : null,

    MaxMarks = es.MaxMarks,
    PassingMarks = es.PassingMarks,

    ExamDate = sch != null ? sch.ExamDate : null,
    StartTime = sch != null ? sch.StartTime : null,
    EndTime = sch != null ? sch.EndTime : null
}
                ).ToListAsync();

                return new ApiResponse<List<ExamSubjectResponseDto>>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<ExamSubjectResponseDto>>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ApiResponse<ExamSchedules>>CreateExamSchedule(CreateExamScheduleDto dto)
        {
            try
            {
                var exam = await _context.Exams
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.ExamId &&
                        x.SchoolId == dto.SchoolId &&
                        x.IsActive);

                if (exam == null)
                {
                    return new ApiResponse<ExamSchedules>
                    {
                        Success = false,
                        Message = "Exam not found for the selected school."
                    };
                }

                if (!exam.StartDate.HasValue || !exam.EndDate.HasValue)
                {
                    return new ApiResponse<ExamSchedules>
                    {
                        Success = false,
                        Message = "The selected exam does not have a valid duration."
                    };
                }

                var examDate = dto.ExamDate.Date;
                if (examDate < exam.StartDate.Value.Date || examDate > exam.EndDate.Value.Date)
                {
                    return new ApiResponse<ExamSchedules>
                    {
                        Success = false,
                        Message = $"Exam date must be between {exam.StartDate.Value:dd MMM yyyy} and {exam.EndDate.Value:dd MMM yyyy}."
                    };
                }

                if (dto.StartTime >= dto.EndTime)
                {
                    return new ApiResponse<ExamSchedules>
                    {
                        Success = false,
                        Message = "Exam end time must be later than the start time."
                    };
                }

                var existingSchedule = await _context.ExamSchedules
                    .FirstOrDefaultAsync(x =>
                        x.IsActive &&
                        x.ExamId == dto.ExamId &&
                        x.SchoolId == dto.SchoolId &&
                        x.ClassId == dto.ClassId &&
                        x.SectionId == dto.SectionId &&
                        x.SubjectId == dto.SubjectId);

                var conflict = await _context.ExamSchedules
                    .AnyAsync(x =>
                        x.IsActive &&
                        (existingSchedule == null || x.Id != existingSchedule.Id) &&
                        x.SchoolId == dto.SchoolId &&
                        x.ClassId == dto.ClassId &&
                        x.SectionId == dto.SectionId &&
                        x.ExamDate.Date == examDate);

                if (conflict)
                {
                    return new ApiResponse<ExamSchedules>
                    {
                        Success = false,
                        Message = "Another subject is already scheduled for this class and section on the selected date."
                    };
                }

                var schedule = existingSchedule ?? new ExamSchedules
                {
                    ExamId = dto.ExamId,
                    SchoolId = dto.SchoolId,
                    ClassId = dto.ClassId,
                    SectionId = dto.SectionId,
                    SubjectId = dto.SubjectId,
                    Status = "Scheduled",
                    IsActive = true
                };

                schedule.ExamDate = examDate;
                schedule.StartTime = dto.StartTime;
                schedule.EndTime = dto.EndTime;

                if (existingSchedule == null)
                {
                    _context.ExamSchedules.Add(schedule);
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<ExamSchedules>
                {
                    Success = true,
                    Message = existingSchedule == null
                        ? "Schedule created successfully."
                        : "Schedule updated successfully.",
                    Data = schedule
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ExamSchedules>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<ExamInvigilators>>AssignInvigilator(AssignInvigilatorDto dto)
        {
            try
            {
                var entity = new ExamInvigilators
                {
                    ExamScheduleId = dto.ExamScheduleId,
                    StaffId = dto.StaffId,
                    DutyType = dto.DutyType
                };

                _context.ExamInvigilators.Add(entity);

                await _context.SaveChangesAsync();

                return new ApiResponse<ExamInvigilators>
                {
                    Success = true,
                    Message = "Invigilator Assigned",
                    Data = entity
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ExamInvigilators>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ApiResponse<List<MarksEntrySheetDto>>>GetMarksEntrySheet(int schoolId, int examId, int sectionId, int subjectId, int userId)
        {
         

            var teacherId = await _context.Staff
                .Where(x => x.usersid == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            try
            {
                var examSessionId = await _context.Exams.Where(x => x.Id == examId && x.SchoolId == schoolId).Select(x => x.AcademicSessionId).FirstOrDefaultAsync();
                var isAllowed =
                    await _context.SectionSubjectTeachers
                    .AnyAsync(x =>
                        x.StaffId == teacherId &&
                        x.SectionId == sectionId &&
                        x.SubjectId == subjectId &&
                        x.SchoolId == schoolId &&
                        x.IsActive);

                if (!isAllowed)
                {
                    return new ApiResponse<List<MarksEntrySheetDto>>
                    {
                        Success = false,
                        Message = "You are not assigned to this subject"
                    };
                }

                var scheduleId = await _context.ExamSchedules.AsNoTracking()
                    .Where(x => x.ExamId == examId && x.SchoolId == schoolId && x.SectionId == sectionId &&
                        x.SubjectId == subjectId && x.IsActive).Select(x => x.Id).FirstOrDefaultAsync();
                if (scheduleId == 0) return new ApiResponse<List<MarksEntrySheetDto>>
                    { Success = false, Message = "Exam schedule not found for this subject." };

                var students =
                    await (
                    from se in _context.StudentEnrollment

                    join st in _context.Students
                    on se.StudentId equals st.Id

                    where se.SectionId == sectionId
                    && se.SchoolId == schoolId
                    && se.SessionId == examSessionId
                    && se.IsActive

                    select new MarksEntrySheetDto
                    {
                        StudentId = st.Id,
                        EnrollmentId = se.Id,
                        StudentName = st.StudentName,
                        RollNumber = se.RollNumber ?? st.Rollnumber,

                        Marks = _context.ExamMarks
                            .Where(m =>
                                m.EnrollmentId == se.Id &&
                                m.ExamId == examId &&
                                m.ExamScheduleId == scheduleId)
                            .Select(m => (decimal?)m.ObtainedMarks)
                            .FirstOrDefault(),

                        Remarks = _context.ExamMarks
                            .Where(m =>
                                m.EnrollmentId == se.Id &&
                                m.ExamId == examId &&
                                m.ExamScheduleId == scheduleId)
                            .Select(m => m.Remarks)
                            .FirstOrDefault()
                    })
                    .OrderBy(x => x.StudentName)
                    .ToListAsync();

                return new ApiResponse<List<MarksEntrySheetDto>>
                {
                    Success = true,
                    Data = students
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<MarksEntrySheetDto>>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> SaveMarks(SaveMarksDto dto, int userId)
        {
            try
            {
                // Get teacherId
                var teacherId = await _context.Staff
                    .Where(x => x.usersid == userId)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                // Check permission
                var isAllowed = await _context.SectionSubjectTeachers
                    .AnyAsync(x =>
                        x.StaffId == teacherId &&
                        x.SectionId == dto.SectionId &&
                        x.SubjectId == dto.SubjectId &&
                        x.SchoolId == dto.SchoolId &&
                        x.IsActive);

                if (!isAllowed)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Unauthorized"
                    };
                }
                var scheduleId = dto.ExamScheduleId;

                if (scheduleId == 0)
                {
                    scheduleId = await _context.ExamSchedules
                        .Where(x =>
                            x.ExamId == dto.ExamId &&
                            x.SectionId == dto.SectionId &&
                            x.SubjectId == dto.SubjectId)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync();
                }

                if (scheduleId == 0)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Exam schedule not found"
                    };
                }
                // Get all existing marks for this exam + schedule + students
                var enrollmentIds = dto.Marks.Select(m => m.EnrollmentId).Where(x => x > 0).ToList();

                var existingMarks = await _context.ExamMarks
                    .Where(x =>
                        x.ExamId == dto.ExamId &&
                        x.ExamScheduleId == scheduleId &&
                        enrollmentIds.Contains(x.EnrollmentId))
                    .ToListAsync();

                // Check if any are locked
                var locked = existingMarks
                    .FirstOrDefault(x => x.IsLocked);

                if (locked != null)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"Marks are locked for StudentId {locked.StudentId}. Update not allowed."
                    };
                }

                // Update existing + insert new
                foreach (var mark in dto.Marks)
                {
                    var existing = existingMarks
                        .FirstOrDefault(x => x.EnrollmentId == mark.EnrollmentId);

                    if (existing != null)
                    {
                        existing.ObtainedMarks = mark.ObtainedMarks;
                        existing.Remarks = mark.Remarks;
                    }
                    else
                    {
                        var entity = new ExamMarks
                        {
                            SchoolId = dto.SchoolId,
                            ExamId = dto.ExamId,
                            ExamScheduleId = scheduleId,
                            StudentId = mark.StudentId,
                            EnrollmentId = mark.EnrollmentId,
                            ObtainedMarks = mark.ObtainedMarks,
                            Remarks = mark.Remarks,
                            EnteredBy = teacherId,
                            EnteredDate = DateTime.UtcNow,
                            IsLocked = false,
                            IsActive = true
                        };

                        _context.ExamMarks.Add(entity);
                    }
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Marks Saved Successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>>LockMarks(int examId, int schoolId)
        {
            try
            {
                var marks =
                    await _context.ExamMarks
                    .Where(x =>
                        x.ExamId == examId &&
                        x.SchoolId == schoolId)
                    .ToListAsync();

                foreach (var item in marks)
                {
                    item.IsLocked = true;
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Marks Locked Successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> GenerateResults(GenerateResultDto dto)
        {
            try
            {
                var exam = await _context.Exams.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.Id == dto.ExamId && x.SchoolId == dto.SchoolId && x.IsActive);
                if (exam == null) return new ApiResponse<string> { Success = false, Message = "Exam not found." };

                var schedules = await _context.ExamSchedules.AsNoTracking()
                    .Where(x => x.ExamId == dto.ExamId && x.SchoolId == dto.SchoolId && x.IsActive && x.SubjectId.HasValue)
                    .Select(x => new { x.Id, x.ClassId, x.SectionId, x.SubjectId }).ToListAsync();
                if (schedules.Count == 0) return new ApiResponse<string> { Success = false, Message = "Add exam schedules before generating results." };

                var enrollments = await _context.StudentEnrollment.AsNoTracking()
                    .Where(x => x.SchoolId == dto.SchoolId && x.SessionId == exam.AcademicSessionId &&
                        x.IsActive && x.EnrollmentStatus == "Active")
                    .Select(x => new { x.Id, x.StudentId, x.ClassId, x.SectionId }).ToListAsync();
                var eligible = enrollments.Where(x => schedules.Any(schedule =>
                    schedule.ClassId == x.ClassId && schedule.SectionId == x.SectionId)).ToList();
                if (eligible.Count == 0) return new ApiResponse<string> { Success = false, Message = "No active students are enrolled in scheduled classes for this exam session." };

                var settings = await _context.ExamSubjects.AsNoTracking()
                    .Where(x => x.ExamId == dto.ExamId && x.SchoolId == dto.SchoolId && x.IsActive)
                    .Select(x => new { x.ClassId, x.SectionId, x.SubjectId, x.MaxMarks, x.PassingMarks }).ToListAsync();
                var enrollmentIds = eligible.Select(x => x.Id).ToList();
                var marks = await _context.ExamMarks.AsNoTracking()
                    .Where(x => x.ExamId == dto.ExamId && x.SchoolId == dto.SchoolId && x.IsActive &&
                        enrollmentIds.Contains(x.EnrollmentId))
                    .Select(x => new { x.EnrollmentId, x.ExamScheduleId, x.ObtainedMarks }).ToListAsync();

                var missing = 0;
                var invalid = 0;
                var summaries = new List<(int EnrollmentId, int StudentId, decimal Total, decimal Obtained, decimal Percentage, string Grade, string Status)>();
                foreach (var enrollment in eligible)
                {
                    var subjectSchedules = schedules.Where(x => x.ClassId == enrollment.ClassId && x.SectionId == enrollment.SectionId)
                        .GroupBy(x => x.SubjectId).Select(group => group.First()).ToList();
                    decimal total = 0;
                    decimal obtained = 0;
                    var passed = true;
                    foreach (var schedule in subjectSchedules)
                    {
                        var setting = settings.Where(x => x.ClassId == enrollment.ClassId && x.SubjectId == schedule.SubjectId &&
                                (x.SectionId == null || x.SectionId == enrollment.SectionId))
                            .OrderByDescending(x => x.SectionId == enrollment.SectionId).FirstOrDefault();
                        if (setting == null || setting.MaxMarks <= 0 || setting.PassingMarks < 0 || setting.PassingMarks > setting.MaxMarks)
                        {
                            invalid++;
                            continue;
                        }
                        var mark = marks.FirstOrDefault(x => x.EnrollmentId == enrollment.Id && x.ExamScheduleId == schedule.Id);
                        if (mark == null) { missing++; continue; }
                        if (mark.ObtainedMarks < 0 || mark.ObtainedMarks > setting.MaxMarks) { invalid++; continue; }
                        total += setting.MaxMarks;
                        obtained += mark.ObtainedMarks;
                        if (mark.ObtainedMarks < setting.PassingMarks) passed = false;
                    }
                    if (total > 0)
                    {
                        var percentage = Math.Round(obtained * 100 / total, 2);
                        summaries.Add((enrollment.Id, enrollment.StudentId, total, obtained, percentage,
                            GetGrade(percentage), passed ? "PASS" : "FAIL"));
                    }
                }
                if (invalid > 0) return new ApiResponse<string> { Success = false,
                    Message = $"Check max marks and entered marks for {invalid} subject entries before generating results." };
                if (missing > 0) return new ApiResponse<string> { Success = false,
                    Message = $"Save marks for all scheduled subjects first. {missing} student-subject entries are missing." };

                foreach (var summary in summaries)
                {
                    var result = await _context.ExamResults.FirstOrDefaultAsync(x => x.SchoolId == dto.SchoolId &&
                        x.ExamId == dto.ExamId && x.EnrollmentId == summary.EnrollmentId);
                    if (result == null)
                    {
                        result = new ExamResults { SchoolId = dto.SchoolId, ExamId = dto.ExamId,
                            StudentId = summary.StudentId, EnrollmentId = summary.EnrollmentId, Published = false };
                        _context.ExamResults.Add(result);
                    }
                    result.TotalMarks = summary.Total;
                    result.ObtainedMarks = summary.Obtained;
                    result.Percentage = summary.Percentage;
                    result.Grade = summary.Grade;
                    result.ResultStatus = summary.Status;
                }
                await _context.SaveChangesAsync();
                return new ApiResponse<string> { Success = true, Message = "Results generated from complete subject marks." };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Success = false, Message = ex.Message };
            }
        }
        private string GetGrade(decimal percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B";
            if (percentage >= 60) return "C";
            if (percentage >= 35) return "D";
            return "F";
        }

        public async Task<ApiResponse<List<StudentResultDto>>> GetResults(int examId, int schoolId)
        {
            try
            {
                var results = await (
                    from r in _context.ExamResults
                    join s in _context.Students
                        on r.StudentId equals s.Id
                    where r.ExamId == examId && r.SchoolId == schoolId
                    select new StudentResultDto
                    {
                        StudentId = s.Id,
                        StudentName = s.StudentName,
                        TotalMarks = r.TotalMarks,
                        ObtainedMarks = r.ObtainedMarks,
                        Percentage = r.Percentage,
                        Grade = r.Grade,
                        Rank = r.RankPosition,
                        ResultStatus = r.ResultStatus
                    }
                ).ToListAsync();

                return new ApiResponse<List<StudentResultDto>>
                {
                    Success = true,
                    Data = results
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<StudentResultDto>>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<StudentResultDto>> GetStudentResult(int studentId, int examId, int schoolId)
        {
            var result = await (
                from r in _context.ExamResults
                join s in _context.Students
                    on r.StudentId equals s.Id
                join e in _context.Exams
                    on r.ExamId equals e.Id
                where r.StudentId == studentId
                      && r.ExamId == examId
                      && r.SchoolId == schoolId
                select new StudentResultDto
                {
                    StudentName = s.StudentName,
                    ExamName = e.Name,
                    TotalMarks = r.TotalMarks,
                    ObtainedMarks = r.ObtainedMarks,
                    Percentage = r.Percentage,
                    Grade = r.Grade,
                    Rank = r.RankPosition,
                    ResultStatus = r.ResultStatus
                }
            ).FirstOrDefaultAsync();

            return new ApiResponse<StudentResultDto>
            {
                Success = true,
                Data = result
            };
        }

        public async Task<ApiResponse<string>> PublishResults(int examId, int schoolId)
        {
            // Recalculate and validate every scheduled subject for every enrolled student
            // before changing any publication flag.
            var generation = await GenerateResults(new GenerateResultDto { ExamId = examId, SchoolId = schoolId });
            if (!generation.Success) return generation;

            var results = await _context.ExamResults
                .Where(x => x.ExamId == examId && x.SchoolId == schoolId)
                .ToListAsync();
            if (results.Count == 0)
                return new ApiResponse<string> { Success = false, Message = "No results were generated." };

            var exam = await _context.Exams.FirstOrDefaultAsync(x => x.Id == examId && x.SchoolId == schoolId && x.IsActive);
            if (exam == null)
                return new ApiResponse<string> { Success = false, Message = "Exam not found." };

            foreach (var result in results) result.Published = true;
            exam.ResultPublished = true;
            await _context.SaveChangesAsync();

            return new ApiResponse<string> { Success = true, Message = "Results published successfully." };
        }
        public async Task<ApiResponse<StudentResultDetailDto>>GetStudentResultDetail(int studentId, int examId, int schoolId)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(x => x.Id == studentId);

                if (student == null)
                {
                    return new ApiResponse<StudentResultDetailDto>
                    {
                        Success = false,
                        Message = "Student not found"
                    };
                }

                var result = await _context.ExamResults
                    .FirstOrDefaultAsync(x =>
                        x.StudentId == studentId &&
                        x.ExamId == examId &&
                        x.SchoolId == schoolId);

                if (result == null)
                {
                    return new ApiResponse<StudentResultDetailDto>
                    {
                        Success = false,
                        Message = "Result not generated"
                    };
                }

                var examName = await _context.Exams
                    .Where(x => x.Id == examId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                var enrollment = await _context.StudentEnrollment.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == result.EnrollmentId && x.StudentId == studentId && x.SchoolId == schoolId);
                if (enrollment == null)
                    return new ApiResponse<StudentResultDetailDto> { Success = false, Message = "Student enrollment not found." };
                var scheduleRows = await (
                    from schedule in _context.ExamSchedules.AsNoTracking()
                    join subject in _context.Subjects.AsNoTracking() on schedule.SubjectId equals subject.Id
                    where schedule.ExamId == examId && schedule.SchoolId == schoolId &&
                        schedule.ClassId == enrollment.ClassId && schedule.SectionId == enrollment.SectionId && schedule.IsActive
                    select new { schedule.Id, SubjectId = subject.Id, SubjectName = subject.SubjectName }
                ).ToListAsync();
                var marks = await _context.ExamMarks.AsNoTracking()
                    .Where(x => x.ExamId == examId && x.SchoolId == schoolId && x.EnrollmentId == result.EnrollmentId && x.IsActive)
                    .Select(x => new { x.ExamScheduleId, x.ObtainedMarks, x.Remarks }).ToListAsync();
                var settings = await _context.ExamSubjects.AsNoTracking()
                    .Where(x => x.ExamId == examId && x.SchoolId == schoolId && x.ClassId == enrollment.ClassId &&
                        (x.SectionId == null || x.SectionId == enrollment.SectionId) && x.IsActive)
                    .Select(x => new { x.SubjectId, x.SectionId, x.MaxMarks, x.PassingMarks }).ToListAsync();
                var subjects = scheduleRows.GroupBy(x => x.SubjectId).Select(group =>
                {
                    var scheduled = group.First();
                    var setting = settings?.Where(x => x.SubjectId == group.Key)
                        .OrderByDescending(x => x.SectionId == enrollment.SectionId).FirstOrDefault();
                    var mark = marks.FirstOrDefault(x => group.Any(schedule => schedule.Id == x.ExamScheduleId));
                    return new StudentSubjectResultDto
                    {
                        SubjectId = group.Key,
                        SubjectName = scheduled.SubjectName,
                        MaxMarks = setting?.MaxMarks ?? 0,
                        PassingMarks = setting?.PassingMarks ?? 0,
                        ObtainedMarks = mark?.ObtainedMarks,
                        Status = mark == null ? "Pending" : mark.ObtainedMarks >= (setting?.PassingMarks ?? 0) ? "PASS" : "FAIL",
                        Remarks = mark?.Remarks
                    };
                }).OrderBy(x => x.SubjectName).ToList();
                var scheduledSubjects = scheduleRows.Select(x => x.SubjectId).Distinct().ToList();
                var school = await _context.Schools.AsNoTracking().Where(x => x.Id == schoolId)
                    .Select(x => new { x.SchoolName, x.Address }).FirstOrDefaultAsync();
                var schoolLogoUrl = await _context.ProfilePictures.AsNoTracking()
                    .Where(x => x.PersonType == "School" && x.PersonId == schoolId && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate).Select(x => x.FileUrl).FirstOrDefaultAsync();
                var className = enrollment == null ? null : await _context.Classes.AsNoTracking()
                    .Where(x => x.Id == enrollment.ClassId).Select(x => x.ClassName).FirstOrDefaultAsync();
                var sectionName = enrollment == null ? null : await _context.SectionDetails.AsNoTracking()
                    .Where(x => x.Id == enrollment.SectionId).Select(x => x.SectionName).FirstOrDefaultAsync();
                var parentName = await _context.ParentDetails.AsNoTracking()
                    .Where(x => x.Id == student.ParentId && x.IsActive).Select(x => x.Name).FirstOrDefaultAsync();
                var isComplete = subjects.Count > 0 && subjects.Count == scheduledSubjects.Count &&
                    subjects.All(x => x.MaxMarks > 0 && x.ObtainedMarks.HasValue) &&
                    subjects.Sum(x => x.MaxMarks) == result.TotalMarks &&
                    subjects.Sum(x => x.ObtainedMarks ?? 0) == result.ObtainedMarks;                var dto = new StudentResultDetailDto
                {
                    StudentId = student.Id,
                    StudentName = student.StudentName,

                    ExamName = examName,
                    SchoolName = school?.SchoolName,
                    SchoolAddress = school?.Address,
                    SchoolLogoUrl = schoolLogoUrl,
                    RollNumber = enrollment?.RollNumber ?? student.Rollnumber,
                    ClassName = className,
                    SectionName = sectionName,
                    ParentName = parentName,
                    ExpectedSubjectCount = scheduledSubjects.Count,
                    RecordedSubjectCount = subjects.Count(x => x.ObtainedMarks.HasValue),
                    IsComplete = isComplete,

                    TotalMarks = result.TotalMarks,
                    ObtainedMarks = result.ObtainedMarks,

                    Percentage = result.Percentage,
                    Grade = result.Grade,
                    ResultStatus = result.ResultStatus,

                    Subjects = subjects
                };

                return new ApiResponse<StudentResultDetailDto>
                {
                    Success = true,
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<StudentResultDetailDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
