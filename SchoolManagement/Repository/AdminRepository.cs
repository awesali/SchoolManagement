using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Repository.SchoolManagement.Repository;
using SchoolManagement.Service;

namespace SchoolManagement.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;
        private readonly ICommonRepository _common;
        private readonly IUserRepository _user;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;


        public AdminRepository(AppDbContext context, IUserRepository user, ICommonRepository common, IWebHostEnvironment env, EmailService emailService)
        {
            _context = context;
            _user = user;
            _common = common;
            _env = env;
            _emailService = emailService;
        }

        public async Task<Schools> CreateSchool(SchoolCreateDto dto, int userId)
        {
            var school = new Schools
            {
                SchoolName = dto.SchoolName,
                Address = dto.Address,
                Email = dto.Email,
                Phone = dto.Phone,
                SuperAdminId = userId,
                Created_By = userId,
                Created_Date = DateTime.Now,
                IsActive = true
            };

            _context.Schools.Add(school);

            await _context.SaveChangesAsync();

            return school;
        }
        public async Task<DashboardCardDto> GetDashboardData(int schoolId)
        {
            var today = DateTime.Today;

            // Total Teachers
            var totalTeachers = await _context.Staff
                .Where(x => x.SchoolId == schoolId && x.IsActive)
                .CountAsync();

            // Teachers Present Today
            var teachersPresent = await _context.StaffAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Present")
                .CountAsync();

            // Total Students
            var totalStudents = await _context.Students
                .Where(x => x.SchoolId == schoolId && x.IsActive)
                .CountAsync();

            // Students Present
            var studentsPresent = await _context.StudentAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Present")
                .CountAsync();

            // Employees On Leave
            var employeesOnLeave = await _context.StaffAttendance
                .Where(x => x.School_Id == schoolId &&
                            x.Attendance_Date == today &&
                            x.Status == "Leave")
                .CountAsync();

            return new DashboardCardDto
            {
                TeachersPresentToday = $"{teachersPresent}/{totalTeachers}",
                StudentsPresentToday = $"{studentsPresent}/{totalStudents}",
                TotalEmployees = totalTeachers,
                EmployeesOnLeave = employeesOnLeave
            };
        }
        public async Task<List<Schools>> GetSchoolsBySuperAdminIdAsync(int superAdminId)
        {
            return await _context.Schools
                .Where(s => s.SuperAdminId == superAdminId && s.IsActive)
                .ToListAsync();
        }
        public async Task<List<StaffListDto>> GetStaffFullAsync(int schoolId)
        {
            return await (from s in _context.Staff
                          join r in _context.Roles on s.RoleId equals r.Id
                          join sc in _context.Schools on s.SchoolId equals sc.Id

                          where s.SchoolId == schoolId

                          select new StaffListDto
                          {
                              Id = s.Id,
                              Name = s.Name,
                              Email = s.Email,
                              Phone = s.Phone,
                              DOB = s.DOB,
                              DOJ = s.DOJ,

                              RoleId = r.Id,
                              RoleName = r.RoleName,

                              SchoolName = sc.SchoolName,
                              Address = s.Adress,
                              IsActive = s.IsActive,

                              Documents = _context.StaffDocuments
                                  .Where(d => d.StaffId == s.Id)
                                  .Select(d => new StaffDocumentDto
                                  {
                                      DocumentId = d.Id,
                                      DocumentName = d.DocumentName,
                                      DocumentURL = d.FileUrl
                                  }).ToList()
                          }).ToListAsync();
        }
        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var doc = await _context.StaffDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (doc == null)
                    return false;

                // ✅ Delete file from server
                if (!string.IsNullOrEmpty(doc.FileUrl))
                {
                    var filePath = Path.Combine(
                        _env.WebRootPath,
                        doc.FileUrl.TrimStart('/')
                    );

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                // ✅ Delete from DB
                _context.StaffDocuments.Remove(doc);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<ApiResponse<Staff>> AddStaffAsync(AddStaffDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ✅ Email check
                var emailExists = await _context.Staff.AnyAsync(e => e.Email == dto.Email && e.SchoolId == dto.SchoolId);
                if (emailExists)
                {
                    return new ApiResponse<Staff>
                    {
                        Success = false,
                        Message = "Email already exists",
                        Data = null
                    };
                }

                // ✅ Create Staff
                var staff = new Staff
                {
                    Name = dto.Name,
                    DOB = dto.DOB,
                    DOJ = dto.DOJ,
                    RoleId = dto.RoleId,
                    SchoolId = dto.SchoolId,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Adress = dto.Address,
                    IsActive = true,
                    Created_Date = DateTime.UtcNow
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                // ✅ Generate Password
                var password = _common.GeneratePassword(dto.Name, dto.DOB);

                // ✅ Register User
                var Req = new RegisterDto
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    RoleId = dto.RoleId,
                    SchoolId = dto.SchoolId,
                    Password = password // 🔥 FIXED
                };

                var result = await _user.Register(Req);

                // ✅ Send Email
                if (result != null)
                {
                    var placeholders = new Dictionary<string, string>
                    {
                        { "Name", dto.Name },
                        { "Email", dto.Email },
                        { "Password", password }
                    };

                    var (subject, body) = await _emailService
                        .GetEmailTemplateAsync("STAFF_CREDENTIALS", placeholders);

                    await _emailService.SendEmailAsync(dto.Email, subject, body);
                }

                // 📁 Folder path
                var folderPath = Path.Combine(_env.WebRootPath, "staffdocs", staff.Id.ToString());

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // ✅ Save Documents
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        var file = dto.Files[i];

                        if (file == null || file.Length == 0)
                            continue;

                        var extension = Path.GetExtension(file.FileName);

                        var inputName = (dto.DocumentNames != null && dto.DocumentNames.Count > i)
                            ? dto.DocumentNames[i]
                            : Path.GetFileNameWithoutExtension(file.FileName);

                        var docName = inputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                            ? inputName
                            : inputName + extension;

                        var uniqueFileName = Guid.NewGuid() + extension;
                        var filePath = Path.Combine(folderPath, uniqueFileName);

                        // ✅ Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // ✅ Save DB record
                        var document = new StaffDocument
                        {
                            StaffId = staff.Id,
                            DocumentName = docName,
                            FileName = file.FileName,
                            FileUrl = $"/staffdocs/{staff.Id}/{uniqueFileName}",
                            CreatedDate = DateTime.UtcNow
                        };

                        _context.StaffDocuments.Add(document);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // ✅ Success Response
                return new ApiResponse<Staff>
                {
                    Success = true,
                    Message = "Staff added successfully",
                    Data = staff
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new ApiResponse<Staff>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }
        public async Task<bool> UpdateStaffAsync(UpdateStaffDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                if (staff == null)
                    return false;

                // ✅ Update Staff
                staff.Name = dto.Name;
                staff.DOB = dto.DOB;
                staff.DOJ = dto.DOJ;
                staff.RoleId = dto.RoleId;
                staff.Email = dto.Email;
                staff.Phone = dto.Phone;
                staff.Adress = dto.Address;
                staff.IsActive = dto.IsActive;
                staff.Modified_Date = DateTime.UtcNow;

                // 📁 Folder path
                var folderPath = Path.Combine(_env.WebRootPath, "staffdocs", staff.Id.ToString());

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // ✅ Existing documents
                var existingDocs = await _context.StaffDocuments
                    .Where(d => d.StaffId == staff.Id)
                    .ToListAsync();

                // ✅ Handle files
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        var file = dto.Files[i];
                        if (file == null || file.Length == 0)
                            continue;

                        var extension = Path.GetExtension(file.FileName);

                        var inputName = (dto.DocumentNames != null && dto.DocumentNames.Count > i)
                            ? dto.DocumentNames[i]
                            : Path.GetFileNameWithoutExtension(file.FileName);

                        var docName = inputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                            ? inputName
                            : inputName + extension;

                        var uniqueFileName = Guid.NewGuid() + extension;
                        var filePath = Path.Combine(folderPath, uniqueFileName);

                        // ✅ Save new file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // 🔥 UPDATE or INSERT
                        if (dto.DocumentIds != null && dto.DocumentIds.Count > i && dto.DocumentIds[i].HasValue)
                        {
                            var existing = existingDocs
                                .FirstOrDefault(d => d.Id == dto.DocumentIds[i].Value);

                            if (existing != null)
                            {
                                // 🔥 DELETE OLD FILE
                                if (!string.IsNullOrEmpty(existing.FileUrl))
                                {
                                    var oldFilePath = Path.Combine(
                                        _env.WebRootPath,
                                        existing.FileUrl.TrimStart('/')
                                    );

                                    if (File.Exists(oldFilePath))
                                    {
                                        File.Delete(oldFilePath);
                                    }
                                }

                                // ✅ UPDATE DB
                                existing.DocumentName = docName;
                                existing.FileName = file.FileName;
                                existing.FileUrl = $"/staffdocs/{staff.Id}/{uniqueFileName}";
                            }
                        }
                        else
                        {
                            // ✅ ADD NEW DOCUMENT
                            var newDoc = new StaffDocument
                            {
                                StaffId = staff.Id,
                                DocumentName = docName,
                                FileName = file.FileName,
                                FileUrl = $"/staffdocs/{staff.Id}/{uniqueFileName}",
                                CreatedDate = DateTime.UtcNow
                            };

                            await _context.StaffDocuments.AddAsync(newDoc);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<RoleDto>> GetRolesBySchoolIdAsync()
        {
            return await _context.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                })
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<List<StudentDto>> GetStudentsBySchoolIdAsync(int schoolId)
        {
            var students = await (
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
                on s.SchoolId equals ac.SchoolId into acGroup
                from ac in acGroup.DefaultIfEmpty()
                where s.SchoolId == schoolId

                select new StudentDto
                {
                    Id = s.Id,
                    StudentName = s.StudentName,
                    DOB = s.DOB,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    ParentId = s.ParentId,
                    SchoolId = s.SchoolId,

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
            ).ToListAsync();

            return students;
        }

        public async Task<StudentDto> GetStudentByIdAsync(int studentId)
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
                on s.SchoolId equals ac.SchoolId into acGroup
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

            return student;
        }

        public async Task<bool> AddStudentAsync(StudentCreateDto dto)
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
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log error here
                return false;
            }
        }


        public async Task<bool> UpdateStudentAsync(StudentUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1️⃣ Get existing student
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                if (student == null)
                    return false;

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
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the error
                return false;
            }
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    return false;

                // Delete student documents from server
                var studentDocs = await _context.Student_Documents
                    .Where(d => d.StudentId == studentId)
                    .ToListAsync();

                foreach (var doc in studentDocs)
                {
                    if (!string.IsNullOrEmpty(doc.FileUrl))
                    {
                        var filePath = Path.Combine(
                            _env.WebRootPath,
                            doc.FileUrl.TrimStart('/')
                        );

                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }

                // Mark student as inactive (soft delete)
                student.IsActive = false;
                student.Modified_Date = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteStudentDocumentAsync(int documentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doc = await _context.Student_Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (doc == null)
                    return false;

                // Delete file from server
                if (!string.IsNullOrEmpty(doc.FileUrl))
                {
                    var filePath = Path.Combine(
                        _env.WebRootPath,
                        doc.FileUrl.TrimStart('/')
                    );

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                // Delete from DB
                _context.Student_Documents.Remove(doc);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<EnrollmentInfoDto>> GetEnrollmentInfoBySchoolAsync(int schoolId)
        {
            var result = await (
                from se in _context.Schools
                join c in _context.Classes on se.Id equals c.SchoolId
                join sd in _context.SectionDetails on se.Id equals sd.SchoolId
                join s in _context.AcademicSessions on se.Id equals s.SchoolId
               // where se.SchoolId == schoolId
                select new EnrollmentInfoDto
                {
                    ClassId = c.Id,
                    ClassName = c.ClassName,
                    SectionId = sd.Id,
                    SectionName = sd.SectionName,
                    SessionId = s.Id,
                    YearStart = s.Year_Start,
                    YearEnd = s.Year_End
                }
            ).Distinct().ToListAsync();

            return result;
        }
    }
}
