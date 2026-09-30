// Backend section: contracts for backend services and repositories.
using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    // Defines the i class operations used by the backend.
    public interface IClassRepository
    {
        Task<ApiResponse<string>> CreateClassWithSectionsAsync(CreateClassWithSectionsDto dto);

        Task<List<ClassDetailDto>> GetClassDetailsBySchoolIdAsync(int schoolId);

        Task<(List<ClassDetailDto> Data, int TotalRecords)> GetClassDetailsPagedAsync(
            int schoolId,
            int page,
            int pageSize
        );

        Task<List<ClassDetailDto>> GetTeacherClassesAsync(int userId);

        Task<ApiResponse<string>> UpdateClassWithSectionsAsync(UpdateClassWithSectionsDto dto);

        Task<ApiResponse<List<SectionSubjectDto>>> GetSubjectsBySectionIdAsync(int sectionId);
    }
}
