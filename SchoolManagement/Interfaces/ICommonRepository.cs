using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface ICommonRepository
    {
        string GeneratePassword(string name, DateTime dob);
        Task<List<StaffDropdownDto>> GetStaffBySchoolIdAsync(int schoolId);
        Task<List<SubjectPicklistDto>> GetSubjectsBySchoolIdAsync(int schoolId);

    }
}
