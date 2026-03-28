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
    }
}
