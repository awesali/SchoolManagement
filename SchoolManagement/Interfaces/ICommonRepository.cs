// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    // Defines the i common operations used by the backend.
    public interface ICommonRepository
    {
        string GeneratePassword(string name, DateTime dob);
        Task<List<StaffDropdownDto>> GetStaffBySchoolIdAsync(int schoolId);
        Task<List<SubjectPicklistDto>> GetSubjectsBySchoolIdAsync(int schoolId);
    }
}
