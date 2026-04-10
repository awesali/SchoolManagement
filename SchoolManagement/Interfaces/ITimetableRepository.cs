using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface ITimetableRepository
    {
        Task<bool> SaveTimetableAsync(SaveTimetableDto dto);

        Task<object> GetTimetableAsync(int sectionId);

        Task<bool> UpdateTimetableAsync(UpdateTimetableDto dto);
    }
}
