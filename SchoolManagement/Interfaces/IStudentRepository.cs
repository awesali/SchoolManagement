using SchoolManagement.DTOs;

namespace SchoolManagement.Interfaces
{
    public interface IStudentRepository
    {
        Task<ApiResponse<string>> UpdateStudentAsync(StudentUpdateDto dto);

        Task<ApiResponse<string>> DeleteStudentDocumentAsync(int documentId);

        Task<(List<StudentDto> Data, int TotalRecords)> GetStudentsBySchoolIdAsync(int schoolId, int page, int pageSize);

        Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int studentId);

        Task<ApiResponse<EnrollmentInfoDto>> GetEnrollmentInfoBySchoolAsync(int schoolId);

        Task<(List<StudentDto> Data, int TotalRecords)> GetStudentsByTeacherIdAsync(int teacherId, int page, int pageSize);

        Task<ApiResponse<string>> MarkAttendanceAsync(MarkBulkAttendanceDto dto);

        Task<(List<AttendanceHistoryDto> Data, int TotalRecords)> GetAttendanceHistoryAsync(int teacherId, DateTime date, int page, int pageSize);

        Task<ApiResponse<string>> AddStudentAsync(StudentCreateDto dto);
    }
}
