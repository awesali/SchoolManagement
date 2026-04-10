using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;

namespace SchoolManagement.Repository
{
    public class TimetableRepository : ITimetableRepository
    {
        private readonly AppDbContext _context;
        public TimetableRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> SaveTimetableAsync(SaveTimetableDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sectionId = dto.SectionId;
                var schoolId = dto.SchoolId;

                // ✅ CHECK: Timetable already exists or not
                var timetableExists = await _context.Timetables
                    .AnyAsync(t => t.SectionId == sectionId
                                && t.SchoolId == schoolId
                                && t.IsActive);

                if (timetableExists)
                {
                    // 👉 Already exists → don't allow overwrite
                    return false; // ya custom response de sakte ho
                }

                // =========================
                // PERIOD VALIDATION
                // =========================
                var periodNumbers = dto.Periods.Select(p => p.PeriodNumber).ToList();
                if (periodNumbers.Count != periodNumbers.Distinct().Count())
                    throw new Exception("Duplicate period numbers found for this section");

                // =========================
                // SAVE PERIODS
                // =========================
                var periods = dto.Periods.Select(p => new TimetablePeriods
                {
                    SectionId = sectionId,
                    PeriodNumber = p.PeriodNumber,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    IsBreak = p.IsBreak
                }).ToList();

                _context.TimetablePeriods.AddRange(periods);
                await _context.SaveChangesAsync();

                // =========================
                // SAVE TIMETABLE SLOTS
                // =========================
                var slots = dto.Days.SelectMany(day => day.Periods.Select(p => new Timetables
                {
                    SectionId = sectionId,
                    DayOfWeek = day.DayOfWeek,
                    PeriodId = p.PeriodId,
                    SubjectId = p.SubjectId,
                    SchoolId = schoolId,
                    IsActive = true,
                    Created_Date = DateTime.UtcNow
                })).ToList();

                _context.Timetables.AddRange(slots);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<object> GetTimetableAsync(int sectionId)
        {
            var periods = await _context.TimetablePeriods
                .Where(p => p.SectionId == sectionId)
                .OrderBy(p => p.PeriodNumber)
                .Select(p => new
                {
                    p.Id,
                    p.SectionId,
                    p.PeriodNumber,
                    p.StartTime,
                    p.EndTime,
                    p.IsBreak
                }).ToListAsync();

            var slots = await _context.Timetables
                .Where(t => t.SectionId == sectionId && t.IsActive)
                .Select(t => new
                {
                    t.DayOfWeek,
                    t.PeriodId,
                    t.SubjectId,
                    SubjectName = t.Subject != null ? t.Subject.SubjectName : null
                }).ToListAsync();

            return new { periods, slots };
        }

        public async Task<bool> UpdateTimetableAsync(UpdateTimetableDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sectionId = dto.SectionId;

                var existingPeriods = await _context.TimetablePeriods
                .Where(p => p.SectionId == sectionId)
                    .ToListAsync();
                _context.TimetablePeriods.RemoveRange(existingPeriods);

                var periodNumbers = dto.Periods.Select(p => p.PeriodNumber).ToList();
                if (periodNumbers.Count != periodNumbers.Distinct().Count())
                    throw new Exception("Duplicate period numbers found");

                var periods = dto.Periods.Select(p => new TimetablePeriods
                {
                    SectionId = sectionId,
                    PeriodNumber = p.PeriodNumber,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    IsBreak = p.IsBreak
                }).ToList();

                _context.TimetablePeriods.AddRange(periods);
                await _context.SaveChangesAsync();

                var existingSlots = await _context.Timetables
                .Where(t => t.SectionId == sectionId && t.IsActive)
                    .ToListAsync();
                _context.Timetables.RemoveRange(existingSlots);

                var slots = dto.Days.SelectMany(day => day.Periods.Select(p => new Timetables
                {
                    SectionId = sectionId,
                    DayOfWeek = day.DayOfWeek,
                    PeriodId = p.PeriodId,
                    SubjectId = p.SubjectId,
                    SchoolId = dto.SchoolId,
                    IsActive = true,
                    Created_Date = DateTime.UtcNow
                })).ToList();

                _context.Timetables.AddRange(slots);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
