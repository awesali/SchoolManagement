using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using static Azure.Core.HttpHeader;

namespace SchoolManagement.Repository
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;
        private readonly ICommonRepository _common;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;



        public StudentRepository(AppDbContext context, ICommonRepository common, IWebHostEnvironment env, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _common = common;
            _env = env;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<ApiResponse<string>> UpdateStudentAsync(StudentUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1️⃣ Get existing student
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                if (student == null)
                    return new ApiResponse<string> { Success = false, Message = "Student not found", Data = null };

                // 2️⃣ Update Parent (if provided)
                var parent = await _context.ParentDetails
                    .FirstOrDefaultAsync(p => p.Id == student.ParentId);

                if (dto.Parent != null && parent != null)
                {
                    parent.Name = dto.Parent.Name ?? parent.Name;
                    parent.PhoneNumber = dto.Parent.PhoneNumber ?? parent.PhoneNumber;
                    parent.Address = dto.Parent.Address ?? parent.Address;
                    parent.Email = dto.Parent.Email ?? parent.Email;
                    parent.Relationship = dto.Parent.Relationship ?? parent.Relationship;
                    parent.Updated_By = 1;
                    parent.Modified_Date = DateTime.Now;
                }

                // 3️⃣ Update Student (only provided fields)
                if (!string.IsNullOrEmpty(dto.StudentName))
                    student.StudentName = dto.StudentName;
                if (dto.DOB.HasValue)
                    student.DOB = dto.DOB.Value;
                if (!string.IsNullOrEmpty(dto.Email))
                    student.Email = dto.Email;
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                    student.PhoneNumber = dto.PhoneNumber; 
                
                if (!string.IsNullOrEmpty(dto.Rollnumber))
                    student.Rollnumber = dto.Rollnumber;

                student.Updated_By = 1;
                student.Modified_Date = DateTime.Now;

                if (dto.IsActive.HasValue)
                    student.IsActive = dto.IsActive.Value;


                student.Updated_By = 1;
                student.Modified_Date = DateTime.Now;
                await _context.SaveChangesAsync();

                // 4️⃣ Update StudentEnrollment (if provided)
                if (dto.ClassId.HasValue || dto.SectionId.HasValue || dto.SessionId.HasValue)
                {
                    var enrollment = await _context.StudentEnrollment
                        .FirstOrDefaultAsync(e => e.StudentId == student.Id);

                    if (enrollment != null)
                    {
                        if (dto.ClassId.HasValue)
                            enrollment.ClassId = dto.ClassId.Value;
                        if (dto.SectionId.HasValue)
                            enrollment.SectionId = dto.SectionId.Value;
                        if (dto.SessionId.HasValue)
                            enrollment.SessionId = dto.SessionId.Value;
                        enrollment.Updated_By = 1;
                        enrollment.Updated_Date = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                // 5️⃣ Handle Student Documents
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    var folderPath = Path.Combine(_env.WebRootPath, "studentdocs", student.Id.ToString());

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var existingDocs = await _context.Student_Documents
                        .Where(d => d.StudentId == student.Id)
                        .ToListAsync();

                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        var file = dto.Files[i];
                        if (file == null || file.Length == 0) continue;

                        var extension = Path.GetExtension(file.FileName);
                        var inputName = (dto.DocumentNames != null && dto.DocumentNames.Count > i)
                            ? dto.DocumentNames[i]
                            : Path.GetFileNameWithoutExtension(file.FileName);

                        var docName = inputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                            ? inputName
                            : inputName + extension;

                        var uniqueFileName = Guid.NewGuid() + extension;
                        var filePath = Path.Combine(folderPath, uniqueFileName);

                        // Save new file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Update existing document if Id provided
                        if (dto.DocumentIds != null && dto.DocumentIds.Count > i && dto.DocumentIds[i].HasValue)
                        {
                            var existing = existingDocs
                                .FirstOrDefault(d => d.Id == dto.DocumentIds[i].Value);

                            if (existing != null)
                            {
                                // Delete old file
                                if (!string.IsNullOrEmpty(existing.FileUrl))
                                {
                                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.FileUrl.TrimStart('/'));
                                    if (File.Exists(oldFilePath))
                                        File.Delete(oldFilePath);
                                }

                                existing.DocumentName = docName;
                                existing.FileName = file.FileName;
                                existing.FileUrl = $"/studentdocs/{student.Id}/{uniqueFileName}";
                            }
                        }
                        else
                        {
                            // Add new document
                            var newDoc = new Student_Documents
                            {
                                StudentId = student.Id,
                                DocumentName = docName,
                                FileName = file.FileName,
                                FileUrl = $"/studentdocs/{student.Id}/{uniqueFileName}",
                                CreatedDate = DateTime.UtcNow
                            };

                            await _context.Student_Documents.AddAsync(newDoc);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                // ✅ Send Update Notification Emails
                // Send email to student (if email was updated or always notify)
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    var studentPlaceholders = new Dictionary<string, string>
                    {
                        { "StudentName", student.StudentName },
                        { "Email", dto.Email },
                        { "SchoolName", "Blue Berry School" }, // You might want to fetch this
                        { "UpdateDate", DateTime.Now.ToString("dd MMM yyyy") }
                    };

                    try
                    {
                        var (studentSubject, studentBody) = await _emailService
                            .GetEmailTemplateAsync("STUDENT_UPDATE", studentPlaceholders);

                        await _emailService.SendEmailAsync(dto.Email, studentSubject, studentBody);
                    }
                    catch
                    {
                        // Log error but don't fail the operation
                    }
                }

                // Send email to parent (if parent email was updated)
                if (dto.Parent?.Email != null && parent != null)
                {
                    var parentPlaceholders = new Dictionary<string, string>
                    {
                        { "ParentName", parent.Name },
                        { "StudentName", student.StudentName },
                        { "Email", dto.Parent.Email },
                        { "SchoolName", "Blue Berry School" },
                        { "UpdateDate", DateTime.Now.ToString("dd MMM yyyy") }
                    };

                    try
                    {
                        var (parentSubject, parentBody) = await _emailService
                            .GetEmailTemplateAsync("PARENT_UPDATE", parentPlaceholders);

                        await _emailService.SendEmailAsync(dto.Parent.Email, parentSubject, parentBody);
                    }
                    catch
                    {
                        // Log error but don't fail the operation
                    }
                }

                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Student updated successfully", Data = null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = ex.Message, Data = null };
            }
        }

        public async Task<ApiResponse<string>> DeleteStudentDocumentAsync(int documentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doc = await _context.Student_Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (doc == null)
                    return new ApiResponse<string> { Success = false, Message = "Student document not found", Data = null };

                if (!string.IsNullOrEmpty(doc.FileUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, doc.FileUrl.TrimStart('/'));
                    if (File.Exists(filePath)) File.Delete(filePath);
                }

                _context.Student_Documents.Remove(doc);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Student document deleted successfully", Data = null };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = "Failed to delete document", Data = null };
            }
        }

        public async Task<(List<StudentDto> Data, int TotalRecords)> GetStudentsBySchoolIdAsync(int schoolId, int page, int pageSize)
        {
            var query = from s in _context.Students
                        join se in _context.StudentEnrollment on s.Id equals se.StudentId into seGroup
                        from se in seGroup.DefaultIfEmpty()
                        join c in _context.Classes on se.ClassId equals c.Id into cGroup
                        from c in cGroup.DefaultIfEmpty()
                        join sd in _context.SectionDetails on se.SectionId equals sd.Id into sdGroup
                        from sd in sdGroup.DefaultIfEmpty()
                        join ac in _context.AcademicSessions on se.SessionId equals ac.Id into acGroup
                        from ac in acGroup.DefaultIfEmpty()
                        where s.SchoolId == schoolId
                        orderby s.Id descending
                        select new StudentDto
                        {
                            Id = s.Id,
                            StudentName = s.StudentName,
                            DOB = s.DOB,
                            Email = s.Email,
                            PhoneNumber = s.PhoneNumber,
                            ParentId = s.ParentId,
                            SchoolId = s.SchoolId,
                            RollNumber = s.Rollnumber,
                            ClassName = c != null ? c.ClassName : null,
                            SectionName = sd != null ? sd.SectionName : null,
                            AcademicSession = ac != null ? ac.Year_Start : (DateTime?)null,
                            IsActive = s.IsActive,
                            Documents = _context.Student_Documents
                                .Where(d => d.StudentId == s.Id)
                                .Select(d => new StudentDocumentDto
                                {
                                    DocumentId = d.Id,
                                    DocumentName = d.DocumentName,
                                    DocumentURL = d.FileUrl,
                                    CreatedDate = d.CreatedDate
                                }).ToList()
                        };

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, total);
        }
        public async Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int studentId)
        {
            var student = await (
                from s in _context.Students

                join se in _context.StudentEnrollment
                    on s.Id equals se.StudentId into seGroup
                from se in seGroup.DefaultIfEmpty()

                join c in _context.Classes
                    on se.ClassId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()

                join sd in _context.SectionDetails
                    on se.SectionId equals sd.Id into sdGroup
                from sd in sdGroup.DefaultIfEmpty()

                join ac in _context.AcademicSessions
                    on se.SessionId equals ac.Id into acGroup  // <-- Join by SessionId, not SchoolId
                from ac in acGroup.DefaultIfEmpty()

                where s.Id == studentId

                select new StudentDto
                {
                    Id = s.Id,
                    StudentName = s.StudentName,
                    DOB = s.DOB,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    ParentId = s.ParentId,
                    SchoolId = s.SchoolId,

                    ClassId = se != null ? se.ClassId : (int?)null,       // <-- Add IDs
                    SectionId = se != null ? se.SectionId : (int?)null,
                    SessionId = se != null ? se.SessionId : (int?)null,

                    ClassName = c != null ? c.ClassName : null,
                    SectionName = sd != null ? sd.SectionName : null,
                    AcademicSession = ac != null ? ac.Year_Start : (DateTime?)null,
                    IsActive = s.IsActive,

                    Documents = _context.Student_Documents
                        .Where(d => d.StudentId == s.Id)
                        .Select(d => new StudentDocumentDto
                        {
                            DocumentId = d.Id,
                            DocumentName = d.DocumentName,
                            DocumentURL = d.FileUrl,
                            CreatedDate = d.CreatedDate
                        }).ToList()
                }
            ).FirstOrDefaultAsync();

            if (student == null)
                return new ApiResponse<StudentDto> { Success = false, Message = "Student not found", Data = null };

            return new ApiResponse<StudentDto> { Success = true, Message = "Student fetched successfully", Data = student };
        }
        

        public async Task<ApiResponse<EnrollmentInfoDto>> GetEnrollmentInfoBySchoolAsync(int schoolId)
        {
            var classes = await _context.Classes
                .Where(c => c.SchoolId == schoolId)
                .Select(c => new ClassDto
                {
                    Id = c.Id,
                    Name = c.ClassName
                }).ToListAsync();

            var sections = await _context.SectionDetails
                .Where(s => s.SchoolId == schoolId)
                .Select(s => new SectionDetailsDto
                {
                    Id = s.Id,
                    Name = s.SectionName,
                    ClassId = s.ClassId
                }).ToListAsync();

            var sessions = await _context.AcademicSessions
                .Where(s => s.SchoolId == schoolId)
                .Select(s => new SessionDto
                {
                    Id = s.Id,
                    YearStart = s.Year_Start,
                    YearEnd = s.Year_End
                }).ToListAsync();

            return new ApiResponse<EnrollmentInfoDto>
            {
                Success = true,
                Message = "Enrollment info fetched successfully",
                Data = new EnrollmentInfoDto { Classes = classes, Sections = sections, Sessions = sessions }
            };
        }

        public async Task<(List<StudentDto> Data, int TotalRecords)> GetStudentsByTeacherIdAsync(int teacherId, int page, int pageSize)
        {
            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == teacherId);
            if (teacher == null)
                return (new List<StudentDto>(), 0);

            var schoolId = teacher.School_Id;

            var query = from se in _context.StudentEnrollment
                        join sd in _context.SectionDetails on se.SectionId equals sd.Id
                        join s in _context.Students on se.StudentId equals s.Id
                        join c in _context.Classes on se.ClassId equals c.Id
                        join ac in _context.AcademicSessions on se.SessionId equals ac.Id
                        where sd.StaffId == teacherId && se.SchoolId == schoolId && sd.SchoolId == schoolId && s.SchoolId == schoolId
                        select new StudentDto
                        {
                            Id = s.Id,
                            StudentName = s.StudentName,
                            DOB = s.DOB,
                            Email = s.Email,
                            PhoneNumber = s.PhoneNumber,
                            ParentId = s.ParentId,
                            SchoolId = s.SchoolId,
                            ClassId = se.ClassId,
                            SectionId = se.SectionId,
                            SessionId = se.SessionId,
                            ClassName = c.ClassName,
                            SectionName = sd.SectionName,
                            AcademicSession = ac.Year_Start,
                            IsActive = s.IsActive,
                            Documents = _context.Student_Documents
                                .Where(d => d.StudentId == s.Id)
                                .Select(d => new StudentDocumentDto
                                {
                                    DocumentId = d.Id,
                                    DocumentName = d.DocumentName,
                                    DocumentURL = d.FileUrl,
                                    CreatedDate = d.CreatedDate
                                }).ToList()
                        };

            var total = await query.CountAsync();
            var data = (await query.ToListAsync()).GroupBy(s => s.Id).Select(g => g.First()).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (data, total);
        }

        //public async Task<ApiResponse<string>> MarkAttendanceAsync(MarkBulkAttendanceDto dto)
        //{
        //    // ✅ Step 0: Get teacherId from claims
        //    var teacherId = int.Parse(_httpContextAccessor.HttpContext.User
        //    .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        //    if (teacherId == 0)
        //        throw new Exception("Unauthorized");

        //    // ✅ Step 1: Get the section and school
        //    var section = await _context.SectionDetails
        //        .FirstOrDefaultAsync(s => s.Id == dto.SectionId && s.StaffId == teacherId);

        //    if (section == null)
        //        throw new Exception("Unauthorized access or invalid section");

        //    int schoolId = section.SchoolId;

        //    // ✅ Step 2: Get valid students for this section
        //    var validStudentIds = await _context.StudentEnrollment
        //        .Where(se => se.SectionId == dto.SectionId)
        //        .Select(se => se.StudentId)
        //        .ToListAsync();

        //    var existingAttendance = await _context.StudentAttendance
        //        .Where(a => a.Attendance_Date.Date == dto.AttendanceDate.Date
        //                    && dto.Students.Select(s => s.StudentId).Contains(a.Student_Id)
        //                    && a.School_Id == schoolId)
        //        .ToListAsync();

        //    //if (existingAttendance.Any())
        //    //    throw new Exception("Attendance has already been marked for some or all students for today");
        //    foreach (var item in dto.Students)
        //    {
        //        // ❌ Skip invalid students
        //        if (!validStudentIds.Contains(item.StudentId))
        //            continue;

        //        var existing = await _context.StudentAttendance
        //            .FirstOrDefaultAsync(a =>
        //                a.Student_Id == item.StudentId &&
        //                a.Attendance_Date.Date == dto.AttendanceDate.Date &&
        //                a.School_Id == schoolId);

        //        if (existing != null)
        //        {
        //            // Update
        //            existing.Status = item.Status;
        //            existing.Updated_By = teacherId;
        //            existing.Updated_Date = DateTime.Now;
        //        }
        //        else
        //        {
        //            // Insert
        //            var attendance = new StudentAttendance
        //            {
        //                Student_Id = item.StudentId,
        //                Attendance_Date = dto.AttendanceDate,
        //                Status = item.Status,
        //                School_Id = schoolId,
        //                Created_At = DateTime.Now,
        //                Created_By = teacherId,
        //                IsActive = true
        //            };

        //            _context.StudentAttendance.Add(attendance);
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //    return new ApiResponse<string> { Success = true, Message = "Attendance marked successfully", Data = null };
        //}

        public async Task<ApiResponse<string>> MarkAttendanceAsync(MarkBulkAttendanceDto dto)
        {
            // ✅ Step 0: Get teacherId from claims
            var teacherId = int.Parse(_httpContextAccessor.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (teacherId == 0)
                return new ApiResponse<string> { Success = false, Message = "Unauthorized" };

            // ✅ Step 1: Validate section
            var section = await _context.SectionDetails
                .FirstOrDefaultAsync(s => s.Id == dto.SectionId && s.StaffId == teacherId);

            if (section == null)
                return new ApiResponse<string> { Success = false, Message = "Unauthorized access or invalid section" };

            int schoolId = section.SchoolId;

            // ✅ Step 2: Check if attendance already exists for this section & date
            var alreadyMarked = await _context.StudentAttendance
                .AnyAsync(a =>
                    a.Attendance_Date.Date == dto.AttendanceDate.Date &&
                    a.School_Id == schoolId &&
                    _context.StudentEnrollment
                        .Where(se => se.SectionId == dto.SectionId)
                        .Select(se => se.StudentId)
                        .Contains(a.Student_Id)
                );

            if (alreadyMarked)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Attendance already marked for this section on selected date"
                };
            }

            // ✅ Step 3: Get valid students
            var validStudentIds = await _context.StudentEnrollment
                .Where(se => se.SectionId == dto.SectionId)
                .Select(se => se.StudentId)
                .ToListAsync();

            // ✅ Step 4: Insert ONLY (no update)
            foreach (var item in dto.Students)
            {
                if (!validStudentIds.Contains(item.StudentId))
                    continue;

                var attendance = new StudentAttendance
                {
                    Student_Id = item.StudentId,
                    Attendance_Date = dto.AttendanceDate,
                    Status = item.Status,
                    School_Id = schoolId,
                    Created_At = DateTime.Now,
                    Created_By = teacherId,
                    IsActive = true
                };

                _context.StudentAttendance.Add(attendance);
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Attendance marked successfully"
            };
        }

        public async Task<(List<AttendanceHistoryDto> Data, int TotalRecords)> GetAttendanceHistoryAsync(int teacherId, DateTime date, int page, int pageSize)
        {
            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == teacherId);
            if (teacher == null)
                throw new Exception("Teacher not found");

            var schoolId = teacher.School_Id;

            var result = await (
                from a in _context.StudentAttendance

                join s in _context.Students
                    on a.Student_Id equals s.Id

                join se in _context.StudentEnrollment
                    on s.Id equals se.StudentId

                join c in _context.Classes
                    on se.ClassId equals c.Id

                join sd in _context.SectionDetails
                    on se.SectionId equals sd.Id

                where sd.StaffId == teacherId
                      && sd.SchoolId == schoolId
                      && a.School_Id == schoolId
                      && a.Attendance_Date.Date == date.Date

                select new AttendanceHistoryDto
                {
                    StudentId = s.Id,
                    StudentName = s.StudentName,
                    SectionId = sd.Id,
                    SectionName = sd.SectionName,
                    AttendanceDate = a.Attendance_Date,
                    Status = a.Status
                }
            )
            .OrderBy(x => x.StudentName)
            .ToListAsync();

            var total = result.Count;
            var data = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (data, total);
        }

        public async Task<ApiResponse<string>> AddStudentAsync(StudentCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Add Parent
                var parent = new ParentDetails
                {
                    Name = dto.Parent.Name,
                    PhoneNumber = dto.Parent.PhoneNumber,
                    Address = dto.Parent.Address,
                    Email = dto.Parent.Email,
                    Relationship = dto.Parent.Relationship,
                    Created_By = 1,
                    Updated_By = 1,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };

                _context.ParentDetails.Add(parent);
                await _context.SaveChangesAsync();

                // 2. Add Student
                var student = new Students
                {
                    StudentName = dto.StudentName,
                    DOB = dto.DOB,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    ParentId = parent.Id,
                    Rollnumber = dto.Rollnumber,
                    SchoolId = dto.SchoolId,
                    Created_By = 1,
                    Updated_By = 1,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // 3. Add StudentEnrollment
                var enrollment = new StudentEnrollment
                {
                    StudentId = student.Id,
                    ClassId = dto.ClassId,
                    SectionId = dto.SectionId,
                    SessionId = dto.SessionId,
                    SchoolId = dto.SchoolId,
                    Created_By = 1,
                    Updated_By = 1,
                    Created_At = DateTime.Now,
                    IsActive = true
                };

                _context.StudentEnrollment.Add(enrollment);
                await _context.SaveChangesAsync();

                // ✅ Generate Passwords and Create Credentials
                var studentPassword = _common.GeneratePassword(dto.StudentName, dto.DOB);
                var parentPassword = _common.GeneratePassword(dto.Parent.Name, dto.DOB);

                // ✅ Create Student Credential
                var studentCred = new Students_Parents_Creds
                {
                    Name = dto.StudentName,
                    Email = dto.Email,
                    Phone = dto.PhoneNumber,
                    Password_Hash = BCrypt.Net.BCrypt.HashPassword(studentPassword),
                    RoleName = "Student",
                    School_Id = dto.SchoolId,
                    Status = "Active",
                    Created_At = DateTime.Now,
                    IsActive = true
                };

                _context.Students_Parents_Creds.Add(studentCred);

                // ✅ Create Parent Credential
                var parentCred = new Students_Parents_Creds
                {
                    Name = dto.Parent.Name,
                    Email = dto.Parent.Email,
                    Phone = dto.Parent.PhoneNumber,
                    Password_Hash = BCrypt.Net.BCrypt.HashPassword(parentPassword),
                    RoleName = "Parent",
                    School_Id = dto.SchoolId,
                    Status = "Active",
                    Created_At = DateTime.Now,
                    IsActive = true
                };

                _context.Students_Parents_Creds.Add(parentCred);
                await _context.SaveChangesAsync();

                // 4. Handle Student Documents
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    var folderPath = Path.Combine(_env.WebRootPath, "studentdocs", student.Id.ToString());

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var existingDocs = await _context.Student_Documents
                        .Where(d => d.StudentId == student.Id)
                        .ToListAsync();

                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        var file = dto.Files[i];
                        if (file == null || file.Length == 0) continue;

                        var extension = Path.GetExtension(file.FileName);

                        var inputName = (dto.DocumentNames != null && dto.DocumentNames.Count > i)
                            ? dto.DocumentNames[i]
                            : Path.GetFileNameWithoutExtension(file.FileName);

                        var docName = inputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                            ? inputName
                            : inputName + extension;

                        var uniqueFileName = Guid.NewGuid() + extension;
                        var filePath = Path.Combine(folderPath, uniqueFileName);

                        // Save new file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Update or Insert
                        if (dto.DocumentIds != null && dto.DocumentIds.Count > i && dto.DocumentIds[i].HasValue)
                        {
                            var existing = existingDocs
                                .FirstOrDefault(d => d.Id == dto.DocumentIds[i].Value);

                            if (existing != null)
                            {
                                // Delete old file
                                if (!string.IsNullOrEmpty(existing.FileUrl))
                                {
                                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.FileUrl.TrimStart('/'));
                                    if (File.Exists(oldFilePath))
                                        File.Delete(oldFilePath);
                                }

                                // Update DB
                                existing.DocumentName = docName;
                                existing.FileName = file.FileName;
                                existing.FileUrl = $"/studentdocs/{student.Id}/{uniqueFileName}";
                            }
                        }
                        else
                        {
                            // Add new document
                            var newDoc = new Student_Documents
                            {
                                StudentId = student.Id,
                                DocumentName = docName,
                                FileName = file.FileName,
                                FileUrl = $"/studentdocs/{student.Id}/{uniqueFileName}",
                                CreatedDate = DateTime.UtcNow
                            };

                            await _context.Student_Documents.AddAsync(newDoc);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                // ✅ Send Welcome Emails
                // Send email to student
                var studentPlaceholders = new Dictionary<string, string>
                {
                    { "StudentName", dto.StudentName },
                    { "Email", dto.Email },
                    { "Password", studentPassword },
                    { "SchoolName", "Blue Berry School" }, // You might want to fetch this
                    { "ClassName", $"Class {dto.ClassId}" },
                    { "ParentName", dto.Parent.Name }
                };

                try
                {
                    var (studentSubject, studentBody) = await _emailService
                        .GetEmailTemplateAsync("STUDENT_WELCOME", studentPlaceholders);

                    await _emailService.SendEmailAsync(dto.Email, studentSubject, studentBody);
                }
                catch
                {
                    // Log error but don't fail the operation
                }

                // Send email to parent
                var parentPlaceholders = new Dictionary<string, string>
                {
                    { "ParentName", dto.Parent.Name },
                    { "StudentName", dto.StudentName },
                    { "Email", dto.Parent.Email },
                    { "Password", parentPassword },
                    { "SchoolName", "Blue Berry School" },
                    { "ClassName", $"Class {dto.ClassId}" }
                };

                try
                {
                    var (parentSubject, parentBody) = await _emailService
                        .GetEmailTemplateAsync("STUDENT_WELCOME", parentPlaceholders);

                    await _emailService.SendEmailAsync(dto.Parent.Email, parentSubject, parentBody);
                }
                catch
                {
                    // Log error but don't fail the operation
                }

                // Commit transaction
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Student added successfully", Data = null };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = ex.Message, Data = null };
            }
        }

        public async Task<IEnumerable<object>> GetStudentsForFees(
            int schoolId,
            int classId,
            int sectionId,
            int sessionId)
        {
            try
            {
                return await (
                    from se in _context.StudentEnrollment

                    join s in _context.Students
                        on se.StudentId equals s.Id

                    join c in _context.Classes
                        on se.ClassId equals c.Id

                    join sec in _context.SectionDetails
                        on se.SectionId equals sec.Id

                    where se.SchoolId == schoolId
                          && s.SchoolId == schoolId
                          && c.SchoolId == schoolId
                          && sec.SchoolId == schoolId

                          && se.ClassId == classId
                          && se.SectionId == sectionId
                          && se.SessionId == sessionId
                          && se.IsActive == true

                    select new
                    {
                        StudentId = s.Id,
                        StudentName = s.StudentName,
                        ClassName = c.ClassName,
                        SectionName = sec.SectionName
                    }

                ).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AssignFeesAsync(AssignFeeDto dto)
        {
            try
            {
                foreach (var studentId in dto.StudentIds)
                {
                    var alreadyExists = await _context.StudentFees
                        .AnyAsync(x =>
                            x.StudentId == studentId
                            && x.FeeTypeId == dto.FeeTypeId
                            && x.SessionId == dto.SessionId
                            && x.IsActive);

                    if (!alreadyExists)
                    {
                        var fee = new StudentFee
                        {
                            StudentId = studentId,
                            FeeTypeId = dto.FeeTypeId,
                            Amount = dto.Amount,
                            SessionId = dto.SessionId,
                            SchoolId = dto.SchoolId,
                            Status = "Pending",
                            Created_Date = DateTime.Now,
                            IsActive = true
                        };

                        _context.StudentFees.Add(fee);
                    }
                }

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetStudentFeesAsync(int studentId)
        {
            try
            {
                return await _context.StudentFees

                    .Where(x => x.StudentId == studentId)

                    .Select(x => new
                    {
                        x.Id,

                        FeeType =
                            _context.FeeTypes
                            .Where(f => f.Id == x.FeeTypeId)
                            .Select(f => f.Name)
                            .FirstOrDefault(),

                        TotalAmount = x.Amount,

                        PaidAmount =
                            _context.FeePayments
                            .Where(p => p.StudentFeeId == x.Id)
                            .Sum(p => (decimal?)p.AmountPaid) ?? 0,

                        Balance =
                            x.Amount -
                            (
                                _context.FeePayments
                                .Where(p => p.StudentFeeId == x.Id)
                                .Sum(p => (decimal?)p.AmountPaid) ?? 0
                            ),

                        x.Status
                    })

                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetPendingFeesAsync(
    int schoolId,
    int? classId,
    int? sectionId)
        {
            try
            {
                var query =

                    from sf in _context.StudentFees

                    join s in _context.Students
                        on sf.StudentId equals s.Id

                    join se in _context.StudentEnrollment
                        on s.Id equals se.StudentId

                    join c in _context.Classes
                        on se.ClassId equals c.Id

                    join sec in _context.SectionDetails
                        on se.SectionId equals sec.Id

                    where sf.SchoolId == schoolId
                          && s.SchoolId == schoolId
                          && se.SchoolId == schoolId
                          && c.SchoolId == schoolId
                          && sec.SchoolId == schoolId

                          && sf.Status != "Paid"
                          && sf.IsActive == true

                    select new
                    {
                        StudentFeeId = sf.Id,

                        StudentId = s.Id,

                        StudentName = s.StudentName,

                        ClassId = c.Id,

                        ClassName = c.ClassName,

                        SectionId = sec.Id,

                        SectionName = sec.SectionName,

                        FeeTypeId = sf.FeeTypeId,

                        Amount = sf.Amount,

                        Paid =
                            _context.FeePayments
                            .Where(p =>
                                p.StudentFeeId == sf.Id
                                && p.SchoolId == schoolId
                                && p.IsActive == true)
                            .Sum(p => (decimal?)p.AmountPaid) ?? 0,

                        Balance =
                            sf.Amount -
                            (
                                _context.FeePayments
                                .Where(p =>
                                    p.StudentFeeId == sf.Id
                                    && p.SchoolId == schoolId
                                    && p.IsActive == true)
                                .Sum(p => (decimal?)p.AmountPaid) ?? 0
                            ),

                        sf.Status
                    };

                // Filter by Class

                if (classId.HasValue)
                {
                    query = query.Where(x => x.ClassId == classId.Value);
                }

                // Filter by Section

                if (sectionId.HasValue)
                {
                    query = query.Where(x => x.SectionId == sectionId.Value);
                }

                return await query.ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<bool> PayFeeAsync(FeePaymentDto dto)
        {
            try
            {
                var payment = new FeePayments
                {
                    StudentFeeId = dto.StudentFeeId,

                    AmountPaid = dto.AmountPaid,

                    Payment_Mode = dto.PaymentMode,

                    Payment_Date = DateTime.Now,

                    Receipt_Number =
                        "RCPT-" + Guid.NewGuid().ToString().Substring(0, 6),

                    SchoolId = dto.SchoolId
                };

                _context.FeePayments.Add(payment);

                await _context.SaveChangesAsync();

                var fee = await _context.StudentFees
                    .FirstOrDefaultAsync(x => x.Id == dto.StudentFeeId);

                var totalPaid = await _context.FeePayments
                    .Where(x => x.StudentFeeId == dto.StudentFeeId)
                    .SumAsync(x => x.AmountPaid);

                if (totalPaid == 0)
                    fee.Status = "Pending";

                else if (totalPaid < fee.Amount)
                    fee.Status = "Partial";

                else
                    fee.Status = "Paid";

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetPaymentHistory(
            int studentId)
        {
            try
            {
                return await (

                    from fp in _context.FeePayments

                    join sf in _context.StudentFees
                        on fp.StudentFeeId equals sf.Id

                    join ft in _context.FeeTypes
                        on sf.FeeTypeId equals ft.Id

                    where sf.StudentId == studentId

                    orderby fp.Payment_Date descending

                    select new
                    {
                        PaymentId = fp.Id,

                        FeeType = ft.Name,

                        AmountPaid = fp.AmountPaid,

                        PaymentDate = fp.Payment_Date,

                        PaymentMode = fp.Payment_Mode,

                        ReceiptNumber = fp.Receipt_Number
                    }

                ).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<object> GetReceipt(int paymentId)
        {
            try
            {
                return await (

                    from fp in _context.FeePayments

                    join sf in _context.StudentFees
                        on fp.StudentFeeId equals sf.Id

                    join s in _context.Students
                        on sf.StudentId equals s.Id

                    join ft in _context.FeeTypes
                        on sf.FeeTypeId equals ft.Id

                    join se in _context.StudentEnrollment
                        on s.Id equals se.StudentId

                    join c in _context.Classes
                        on se.ClassId equals c.Id

                    join sec in _context.SectionDetails
                        on se.SectionId equals sec.Id

                    where fp.Id == paymentId

                    select new
                    {
                        ReceiptNumber = fp.Receipt_Number,

                        PaymentDate = fp.Payment_Date,

                        StudentName = s.StudentName,

                        ClassName = c.ClassName,

                        SectionName = sec.SectionName,

                        FeeType = ft.Name,

                        AmountPaid = fp.AmountPaid,

                        PaymentMode = fp.Payment_Mode
                    }

                ).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CreateFeeType(FeeType dto)
        {
            try
            {
                _context.FeeTypes.Add(dto);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetFeeTypes(int schoolId)
        {
            try
            {
                return await _context.FeeTypes
                    .Where(x => x.SchoolId == schoolId && x.IsActive)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name
                    })
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
