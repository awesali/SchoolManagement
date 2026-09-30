// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    // Defines the i timetable operations used by the backend.
    public interface ITimetableRepository
    {
        Task<bool> SaveTimetableAsync(SaveTimetableDto dto);

        Task<object> GetTimetableAsync(int sectionId);

        Task<bool> UpdateTimetableAsync(UpdateTimetableDto dto);
    }
}
