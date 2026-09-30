// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    // Defines the i subject operations used by the backend.
    public interface ISubjectRepository
    {
        Task<(List<SubjectDto> Data, int TotalRecords)> GetSubjectsBySchoolIdAsync(
            int schoolId,
            int page,
            int pageSize
        );

        Task<ApiResponse<SubjectDto>> AddSubjectAsync(AddSubjectDto dto);

        Task<ApiResponse<string>> UpdateSubjectAsync(UpdateSubjectDto dto);

        Task<ApiResponse<string>> AssignSubjectsToSectionAsync(AssignSubjectToSectionDto dto);

        Task<List<GetSectionSubjectDto>> GetSubjectsBySectionAsync(int sectionId, int schoolId);
    }
}
