using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IExamRepository
    {
        // Exam
        Task CreateExamSchedulesAsync(CreateExamScheduleRequest request);

        // Invigilator
        Task<int> AssignInvigilatorAsync(AssignInvigilatorDto dto);
        Task<List<ExamInvigilators>> GetInvigilatorsByScheduleAsync(int scheduleId);

        // ExamType Picklist
        Task<List<ExamTypePicklistDto>> GetExamTypePicklistAsync(int schoolId);

        // Scheduled Exams List
        Task<(List<ExamScheduleListDto> Data, int Total)> GetScheduledExamsAsync(int schoolId, int page, int pageSize);

        // Exam Detail by ExamId and SchoolId
        Task<ExamDetailDto?> GetExamDetailAsync(int examId, int schoolId);

        // Publish
        //Task<bool> PublishExamAsync(int examId);
    }
}
