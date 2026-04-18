using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface IClassRepository
    {
        Task<ApiResponse<string>> CreateClassWithSectionsAsync(CreateClassWithSectionsDto dto);

        Task<List<ClassDetailDto>> GetClassDetailsBySchoolIdAsync(int schoolId);

        Task<(List<ClassDetailDto> Data, int TotalRecords)> GetClassDetailsPagedAsync(int schoolId, int page, int pageSize);

        Task<ApiResponse<string>> UpdateClassWithSectionsAsync(UpdateClassWithSectionsDto dto);

        Task<ApiResponse<List<SectionSubjectDto>>> GetSubjectsBySectionIdAsync(int sectionId);
    }
}
