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
        //    // 🔥 Step 1: Create Exam (GROUP)
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
        //    await _context.SaveChangesAsync(); // 👉 ExamId mil gaya

        //    var schedules = new List<ExamSchedules>();

        //    // 🔥 Step 2: Add schedules under SAME ExamId
        //    foreach (var cls in request.Classes)
        //    {
        //        foreach (var sec in cls.Sections)
        //        {
        //            foreach (var sub in sec.Subjects)
        //            {
        //                schedules.Add(new ExamSchedules
        //                {
        //                    ExamId = exam.Id,   // ✅ GROUPING FIX
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

        //    // 🔥 STAFF CONFLICT CHECK
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

        //                    // 🔥 Count distinct classes
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
                                m.ExamId == examId)
                            .Select(m => (decimal?)m.ObtainedMarks)
                            .FirstOrDefault(),

                        Remarks = _context.ExamMarks
                            .Where(m =>
                                m.EnrollmentId == se.Id &&
                                m.ExamId == examId)
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
                var enrollments = await _context.ExamMarks
                    .Where(x => x.ExamId == dto.ExamId && x.SchoolId == dto.SchoolId)
                    .Select(x => new { x.EnrollmentId, x.StudentId })
                    .Distinct()
                    .ToListAsync();

                foreach (var enrollment in enrollments)
                {
                    var marks = await (
                        from em in _context.ExamMarks
                        join es in _context.ExamSchedules
                            on em.ExamScheduleId equals es.Id
                        join sub in _context.Subjects
                            on es.SubjectId equals sub.Id
                        where em.EnrollmentId == enrollment.EnrollmentId
                              && em.ExamId == dto.ExamId
                        select new
                        {
                            em.ObtainedMarks,
                            es.SubjectId
                        }
                    ).ToListAsync();

                    var subjectTotals = await _context.ExamSchedules
                        .Where(x => x.ExamId == dto.ExamId)
                        .ToListAsync();

                    decimal obtained = marks.Sum(x => x.ObtainedMarks);
                    decimal total = subjectTotals.Count * 100; // simplified OR use ExamSubjects

                    decimal percentage = (obtained / total) * 100;

                    string grade = GetGrade(percentage);

                    string status = marks.Any(x => x.ObtainedMarks < 35)
                        ? "FAIL"
                        : "PASS";

                    var existing = await _context.ExamResults
                        .FirstOrDefaultAsync(x =>
                            x.ExamId == dto.ExamId &&
                            x.EnrollmentId == enrollment.EnrollmentId);

                    if (existing != null)
                    {
                        existing.TotalMarks = total;
                        existing.ObtainedMarks = obtained;
                        existing.Percentage = percentage;
                        existing.Grade = grade;
                        existing.ResultStatus = status;
                    }
                    else
                    {
                        _context.ExamResults.Add(new ExamResults
                        {
                            SchoolId = dto.SchoolId,
                            ExamId = dto.ExamId,
                            StudentId = enrollment.StudentId,
                            EnrollmentId = enrollment.EnrollmentId,
                            TotalMarks = total,
                            ObtainedMarks = obtained,
                            Percentage = percentage,
                            Grade = grade,
                            ResultStatus = status,
                            Published = false
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Results Generated Successfully"
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
            var results = await _context.ExamResults
                .Where(x => x.ExamId == examId && x.SchoolId == schoolId)
                .ToListAsync();

            foreach (var r in results)
            {
                r.Published = true;
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId);

            exam.ResultPublished = true;

            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Results Published Successfully"
            };
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

                var subjects = await (
                    from em in _context.ExamMarks

                    join esch in _context.ExamSchedules
                        on em.ExamScheduleId equals esch.Id

                    join sub in _context.Subjects
                        on esch.SubjectId equals sub.Id

                    from exSub in _context.ExamSubjects
                        .Where(x =>
                            x.ExamId == esch.ExamId &&
                            x.SubjectId == esch.SubjectId &&
                            x.ClassId == esch.ClassId &&
                            x.SectionId == esch.SectionId)

                    where em.StudentId == studentId
                          && em.ExamId == examId

                    select new StudentSubjectResultDto
                    {
                        SubjectId = sub.Id,
                        SubjectName = sub.SubjectName,
                        MaxMarks = exSub.MaxMarks,
                        PassingMarks = exSub.PassingMarks,
                        ObtainedMarks = em.ObtainedMarks,
                        Status = em.ObtainedMarks >= exSub.PassingMarks
                            ? "PASS"
                            : "FAIL",
                        Remarks = em.Remarks
                    }
                ).ToListAsync();

                var dto = new StudentResultDetailDto
                {
                    StudentId = student.Id,
                    StudentName = student.StudentName,

                    ExamName = examName,

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
