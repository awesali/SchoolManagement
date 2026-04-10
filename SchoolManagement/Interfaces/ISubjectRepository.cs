using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface ISubjectRepository
    {
        Task<(List<SubjectDto> Data, int TotalRecords)> GetSubjectsBySchoolIdAsync(int schoolId, int page, int pageSize);

        Task<ApiResponse<SubjectDto>> AddSubjectAsync(AddSubjectDto dto);

        Task<ApiResponse<string>> UpdateSubjectAsync(UpdateSubjectDto dto);

        Task<ApiResponse<string>> AssignSubjectsToSectionAsync(AssignSubjectToSectionDto dto);

    }
}
