using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    public class ExamRepository : IExamRepository
    {
        private readonly AppDbContext _context;

        public ExamRepository(AppDbContext context)
        {
            _context = context;
        }

        // ---------------- CREATE EXAM ----------------
        public async Task CreateExamSchedulesAsync(CreateExamScheduleRequest request)
        {
            // 🔥 Step 1: Create Exam (GROUP)
            var exam = new Exams
            {
                Name = request.Name, // e.g. "Nursery Midterm April"
                ExamTypeId = request.ExamTypeId,
                SchoolId = request.SchoolId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsPublished = false
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync(); // 👉 ExamId mil gaya

            var schedules = new List<ExamSchedules>();

            // 🔥 Step 2: Add schedules under SAME ExamId
            foreach (var cls in request.Classes)
            {
                foreach (var sec in cls.Sections)
                {
                    foreach (var sub in sec.Subjects)
                    {
                        schedules.Add(new ExamSchedules
                        {
                            ExamId = exam.Id,   // ✅ GROUPING FIX
                            ExamTypeId = request.ExamTypeId,
                            SchoolId = request.SchoolId,
                            ClassId = cls.ClassId,
                            SectionId = sec.SectionId,
                            SubjectId = sub.SubjectId,
                            ExamDate = sub.ExamDate,
                            StartTime = sub.StartTime,
                            EndTime = sub.EndTime
                        });
                    }
                }
            }

            await _context.ExamSchedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();
        }

        // ---------------- ASSIGN INVIGILATOR ----------------
        public async Task<int> AssignInvigilatorAsync(AssignInvigilatorDto dto)
        {
            var schedule = await _context.ExamSchedules
                .FirstOrDefaultAsync(x => x.Id == dto.ExamScheduleId);

            if (schedule == null)
                throw new Exception("Schedule not found");

            // 🔥 STAFF CONFLICT CHECK
            var conflict = await _context.ExamInvigilators
                .Include(x => x.ExamSchedule)
                .AnyAsync(x =>
                    x.StaffId == dto.StaffId &&
                    x.ExamSchedule.ExamDate == schedule.ExamDate &&
                    (
                        (schedule.StartTime >= x.ExamSchedule.StartTime && schedule.StartTime < x.ExamSchedule.EndTime) ||
                        (schedule.EndTime > x.ExamSchedule.StartTime && schedule.EndTime <= x.ExamSchedule.EndTime)
                    )
            );

            if (conflict)
                throw new Exception("Staff already assigned in another exam");

            var inv = new ExamInvigilators
            {
                ExamScheduleId = dto.ExamScheduleId,
                StaffId = dto.StaffId,
                DutyType = dto.DutyType
            };

            _context.ExamInvigilators.Add(inv);
            await _context.SaveChangesAsync();

            return inv.Id;
        }

     
        // ---------------- EXAM TYPE PICKLIST ----------------
        public async Task<List<ExamTypePicklistDto>> GetExamTypePicklistAsync(int schoolId)
        {
            return await _context.ExamTypes
                .Where(x => x.IsActive && x.schoolId == schoolId)
                .Select(x => new ExamTypePicklistDto { Id = x.Id, Name = x.Name })
                .ToListAsync();
        }

        // ---------------- PUBLISH ----------------
        //public async Task<bool> PublishExamAsync(int examId)
        //{
        //    var exam = await _context.Exams.FindAsync(examId);

        //    if (exam == null)
        //        return false;

        //    exam.IsPublished = true;
        //    await _context.SaveChangesAsync();

        //    return true;
        //}

        // ---------------- SCHEDULED EXAMS LIST ----------------
        public async Task<(List<ExamScheduleListDto> Data, int Total)> GetScheduledExamsAsync(int schoolId, int page, int pageSize)
        {
            var query = from e in _context.Exams
                        join et in _context.ExamTypes on e.ExamTypeId equals et.Id
                        where e.SchoolId == schoolId
                        select new ExamScheduleListDto
                        {
                            ExamId = e.Id,
                            ExamName = e.Name,
                            ExamTitle = et.Name,
                            StartDate = e.StartDate,
                            EndDate = e.EndDate,

                            // 🔥 Count distinct classes
                            ClassCount = _context.ExamSchedules
                                .Where(es => es.ExamId == e.Id)
                                .Select(es => es.ClassId)
                                .Distinct()
                                .Count()
                        };

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }

        // ---------------- EXAM DETAIL ----------------
        public async Task<ExamDetailDto?> GetExamDetailAsync(int examId, int schoolId)
        {
            var exam = await _context.Exams
                .Where(e => e.Id == examId && e.SchoolId == schoolId)
                .Join(_context.ExamTypes, e => e.ExamTypeId, et => et.Id,
                    (e, et) => new { e, et })
                .FirstOrDefaultAsync();

            if (exam == null) return null;

            var schedules = await (
                from es in _context.ExamSchedules
                join c in _context.Classes on es.ClassId equals c.Id
                join s in _context.SectionDetails on es.SectionId equals s.Id
                join sub in _context.Subjects on es.SubjectId equals sub.Id
                where es.ExamId == examId && es.SchoolId == schoolId
                select new
                {
                    es.ClassId, c.ClassName,
                    es.SectionId, s.SectionName,
                    es.SubjectId, sub.SubjectName,
                    es.ExamDate, es.StartTime, es.EndTime
                }
            ).ToListAsync();

            var classes = schedules
                .GroupBy(x => new { x.ClassId, x.ClassName })
                .Select(cg => new ExamDetailClassDto
                {
                    ClassId = cg.Key.ClassId,
                    ClassName = cg.Key.ClassName,
                    Sections = cg.GroupBy(x => new { x.SectionId, x.SectionName })
                        .Select(sg => new ExamDetailSectionDto
                        {
                            SectionId = sg.Key.SectionId,
                            SectionName = sg.Key.SectionName,
                            Subjects = sg.Select(x => new ExamDetailSubjectDto
                            {
                                SubjectId = x.SubjectId,
                                SubjectName = x.SubjectName,
                                ExamDate = x.ExamDate,
                                StartTime = x.StartTime,
                                EndTime = x.EndTime
                            }).ToList()
                        }).ToList()
                }).ToList();

            return new ExamDetailDto
            {
                ExamId = exam.e.Id,
                ExamName = exam.e.Name,
                ExamType = exam.et.Name,
                StartDate = exam.e.StartDate,
                EndDate = exam.e.EndDate,
                Classes = classes
            };
        }

        public async Task<List<ExamInvigilators>> GetInvigilatorsByScheduleAsync(int scheduleId)
        {
            return await _context.ExamInvigilators
                .Include(x => x.ExamSchedule)
                .Where(x => x.ExamScheduleId == scheduleId)
                .ToListAsync();
        }
    }
}
