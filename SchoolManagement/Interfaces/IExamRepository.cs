using SchoolManagement.DTOs;
using SchoolManagement.Model;

namespace SchoolManagement.Interfaces
{
    public interface IExamRepository
    {
        // Exam
        //Task CreateExamSchedulesAsync(CreateExamScheduleRequest request);

        //// Invigilator
        //Task<int> AssignInvigilatorAsync(AssignInvigilatorDto dto);
        //Task<List<ExamInvigilators>> GetInvigilatorsByScheduleAsync(int scheduleId);

        //// ExamType Picklist
        //Task<List<ExamTypePicklistDto>> GetExamTypePicklistAsync(int schoolId);

        //// Scheduled Exams List
        //Task<(List<ExamScheduleListDto> Data, int Total)> GetScheduledExamsAsync(int schoolId, int page, int pageSize);

        //// Exam Detail by ExamId and SchoolId
        //Task<ExamDetailDto?> GetExamDetailAsync(int examId, int schoolId);

        //// Publish
        //Task<bool> PublishExamAsync(int examId);

        Task<ApiResponse<ExamTypes>>
            CreateExamType(CreateExamTypeDto dto, int userId);

        Task<ApiResponse<List<ExamTypes>>>
            GetExamTypes(int schoolId);

        Task<ApiResponse<Exams>>
            CreateExam(CreateExamDto dto, int userId);

        Task<ApiResponse<List<Exams>>>
            GetExams(int schoolId);

        Task<ApiResponse<Exams>>
            PublishExam(int examId);

        Task<ApiResponse<ExamSubjects>>
       AddExamSubject(AddExamSubjectDto dto, int userId);

        Task<ApiResponse<List<ExamSubjectResponseDto>>>
            GetExamSubjects(int examId);

        Task<ApiResponse<ExamSchedules>>
CreateExamSchedule(
    CreateExamScheduleDto dto);

        Task<ApiResponse<ExamInvigilators>>
AssignInvigilator(
    AssignInvigilatorDto dto);


        Task<ApiResponse<List<MarksEntrySheetDto>>>
            GetMarksEntrySheet(
                int schoolId,
                int examId,
                int sectionId,
                int subjectId,
                int teacherId);

        Task<ApiResponse<string>>
            SaveMarks(
                SaveMarksDto dto,
                int teacherId);

        Task<ApiResponse<string>>
            LockMarks(
                int examId,
                int schoolId);

        Task<ApiResponse<string>> GenerateResults(GenerateResultDto dto);

        Task<ApiResponse<List<StudentResultDto>>> GetResults(int examId, int schoolId);

        Task<ApiResponse<StudentResultDto>> GetStudentResult(int studentId, int examId, int schoolId);

        Task<ApiResponse<string>> PublishResults(int examId, int schoolId);
    }
}
