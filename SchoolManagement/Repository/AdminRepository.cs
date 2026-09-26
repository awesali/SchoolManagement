using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Repository.SchoolManagement.Repository;
using SchoolManagement.Service;
using System.Security.Claims;
using System.Xml;

namespace SchoolManagement.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;
        private readonly ICommonRepository _common;
        private readonly IUserRepository _user;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminRepository(AppDbContext context, IUserRepository user, ICommonRepository common, IWebHostEnvironment env, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _user = user;
            _common = common;
            _env = env;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        private static string? ValidateSchoolLogo(IFormFile? logo)
        {
            if (logo == null) return null;
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(logo.ContentType.ToLowerInvariant())) return "School logo must be a JPG, PNG, or WebP image.";
            if (logo.Length == 0 || logo.Length > 5 * 1024 * 1024) return "School logo must be between 1 byte and 5 MB.";
            return null;
        }

        private async Task<ProfilePicture> SaveSchoolLogoAsync(int schoolId, IFormFile logo)
        {
            var extension = logo.ContentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", "school-logos");
            Directory.CreateDirectory(folder);
            var fileName = $"school-{schoolId}-{Guid.NewGuid():N}{extension}";
            await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew);
            await logo.CopyToAsync(stream);
            return new ProfilePicture { PersonType = "School", PersonId = schoolId, FileName = fileName,
                FileUrl = $"/uploads/school-logos/{fileName}", ContentType = logo.ContentType,
                CreatedDate = DateTime.UtcNow, IsActive = true };
        }

        public async Task<ApiResponse<Schools>> CreateSchool(SchoolCreateDto dto, int userId)
        {
            var logoError = ValidateSchoolLogo(dto.Logo);
            if (logoError != null) return new ApiResponse<Schools> { Success = false, Message = logoError };
            ProfilePicture? savedLogo = null;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var school = new Schools
                {
                    SchoolName = dto.SchoolName,
                    Address = string.IsNullOrWhiteSpace(dto.Address)
                        ? BuildAddress(dto)
                        : dto.Address,
                    Street = dto.Street,
                    City = dto.City,
                    PinCode = dto.PinCode,
                    Country = dto.Country,
                    State = dto.State,
                    Landmark = dto.Landmark,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    SuperAdminId = userId,
                    Created_By = userId,
                    Created_Date = DateTime.Now,
                    IsActive = true
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                if (dto.Logo != null)
                {
                    savedLogo = await SaveSchoolLogoAsync(school.Id, dto.Logo);
                    _context.ProfilePictures.Add(savedLogo);
                    await _context.SaveChangesAsync();
                    school.LogoUrl = savedLogo.FileUrl;
                }

                await transaction.CommitAsync();

                return new ApiResponse<Schools> { Success = true, Message = "School created successfully", Data = school };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (savedLogo != null)
                {
                    var path = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), savedLogo.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(path)) File.Delete(path);
                }
                return new ApiResponse<Schools> { Success = false, Message = ex.Message, Data = null };
            }
        }

        private static string BuildAddress(SchoolCreateDto dto)
        {
            return string.Join(", ", new[]
            {
                dto.Street,
                dto.Landmark,
                dto.City,
                dto.State,
                dto.PinCode,
                dto.Country
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        public async Task<ApiResponse<DashboardCardDto>> GetDashboardData(int schoolId)
        {
            var today = DateTime.Today;

            var totalTeachers = await _context.Staff.Where(x => x.SchoolId == schoolId && x.IsActive).CountAsync();
            var teachersPresent = await _context.StaffAttendance.Where(x => x.School_Id == schoolId && x.Attendance_Date == today && x.Status == "Present").CountAsync();
            var totalStudents = await _context.Students.Where(x => x.SchoolId == schoolId && x.IsActive).CountAsync();
            var studentsPresent = await _context.StudentAttendance.Where(x => x.School_Id == schoolId && x.Attendance_Date == today && x.Status == "Present").CountAsync();
            var employeesOnLeave = await _context.StaffAttendance.Where(x => x.School_Id == schoolId && x.Attendance_Date == today && x.Status == "Leave").CountAsync();

            return new ApiResponse<DashboardCardDto>
            {
                Success = true,
                Message = "Dashboard data fetched successfully",
                Data = new DashboardCardDto
                {
                    TeachersPresentToday = $"{teachersPresent}/{totalTeachers}",
                    StudentsPresentToday = $"{studentsPresent}/{totalStudents}",
                    TotalEmployees = totalTeachers,
                    EmployeesOnLeave = employeesOnLeave
                }
            };
        }

        public async Task<ApiResponse<List<Schools>>> GetSchoolsBySuperAdminIdAsync(int superAdminId)
        {
            var data = await _context.Schools
                .Where(s => s.SuperAdminId == superAdminId && s.IsActive)
                .OrderByDescending(s => s.Created_Date)
                .ToListAsync();

            var schoolIds = data.Select(x => x.Id).ToList();
            var logos = await _context.ProfilePictures.AsNoTracking()
                .Where(x => x.PersonType == "School" && x.IsActive && schoolIds.Contains(x.PersonId))
                .OrderByDescending(x => x.CreatedDate).ToListAsync();
            foreach (var school in data)
                school.LogoUrl = logos.FirstOrDefault(x => x.PersonId == school.Id)?.FileUrl;

            return new ApiResponse<List<Schools>> { Success = true, Message = "Schools fetched successfully", Data = data };
        }

        public async Task<(List<StaffListDto> Data, int TotalRecords)> GetStaffFullAsync(int schoolId, int page, int pageSize, int? staffId = null)
        {
            var query = from s in _context.Staff
                        join r in _context.Roles on s.RoleId equals r.Id
                        join sc in _context.Schools on s.SchoolId equals sc.Id
                        where s.SchoolId == schoolId && (!staffId.HasValue || s.Id == staffId.Value)
                        orderby s.Id descending
                        select new StaffListDto
                        {
                            Id = s.Id,
                            // Legacy staff rows may not have a linked user yet.
                            EmployeeNumber = EF.Property<int?>(s, nameof(Staff.usersid)) ?? 0,
                            Name = s.Name,
                            Email = s.Email,
                            Phone = s.Phone,
                            DOB = s.DOB,
                            GenderCode = s.GenderCode,
                            DOJ = s.DOJ,
                            RoleId = r.Id,
                            RoleName = r.RoleName,
                            EmploymentType = s.EmploymentType,
                            SchoolName = sc.SchoolName,
                            Address = s.Adress,
                            AddressLine2 = s.AddressLine2,
                            Landmark = s.Landmark,
                            City = s.City,
                            District = s.District,
                            State = s.State,
                            Country = s.Country,
                            PinCode = s.PinCode,
                            Qualification = s.Qualification,
                            Specialization = s.Specialization,
                            Institute = s.Institute,
                            University = s.University,
                            PassingYear = s.PassingYear,
                            Grade = s.Grade,
                            PreviousEmployer = s.PreviousEmployer,
                            PreviousDesignation = s.PreviousDesignation,
                            ExperienceYears = s.ExperienceYears,
                            ExperienceFrom = s.ExperienceFrom,
                            ExperienceTo = s.ExperienceTo,
                            ExperienceDetails = s.ExperienceDetails,
                            AdditionalDetails = s.AdditionalDetails,
                            CertificationName = s.CertificationName,
                            CertificationIssuer = s.CertificationIssuer,
                            CertificationNumber = s.CertificationNumber,
                            CertificationDate = s.CertificationDate,
                            CertificationExpiry = s.CertificationExpiry,
                            IsActive = s.IsActive,
                            ProfilePictureUrl = _context.ProfilePictures
                                .Where(p => p.PersonType == "Staff" && p.PersonId == s.Id && p.IsActive)
                                .Select(p => p.FileUrl)
                                .FirstOrDefault(),
                            Documents = _context.StaffDocuments
                                .Where(d => d.StaffId == s.Id)
                                .Select(d => new StaffDocumentDto
                                {
                                    DocumentId = d.Id,
                                    DocumentName = d.DocumentName,
                                    DocumentURL = d.FileUrl
                                }).ToList()
                        };

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, total);
        }

        public async Task<List<string>> GetStaffEmailsAsync(int schoolId)
        {
            return await _context.Staff
                .AsNoTracking()
                .Where(s => s.SchoolId == schoolId && s.Email != null)
                .Select(s => s.Email)
                .Distinct()
                .ToListAsync();
        }

        public async Task<ApiResponse<string>> DeleteDocumentAsync(int documentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var doc = await _context.StaffDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (doc == null)
                    return new ApiResponse<string> { Success = false, Message = "Document not found", Data = null };

                if (!string.IsNullOrEmpty(doc.FileUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, doc.FileUrl.TrimStart('/'));
                    if (File.Exists(filePath)) File.Delete(filePath);
                }

                _context.StaffDocuments.Remove(doc);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ApiResponse<string> { Success = true, Message = "Document deleted successfully", Data = null };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ApiResponse<string> { Success = false, Message = "Failed to delete document", Data = null };
            }
        }

        public async Task<ApiResponse<Staff>> AddStaffAsync(AddStaffDto dto)
        {
            var employmentTypes = new[] { "Permanent", "Contract", "Part-Time", "Temporary", "Intern", "Substitute", "Probationary", "Visiting / Guest", "Consultant", "Volunteer" };
            dto.EmploymentType = employmentTypes.FirstOrDefault(type => string.Equals(type, dto.EmploymentType?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (dto.EmploymentType == null)
                return new ApiResponse<Staff> { Success = false, Message = "Select a valid employment type." };
            if (dto.PassingYear.HasValue && (dto.PassingYear < 1900 || dto.PassingYear > DateTime.Today.Year))
                return new ApiResponse<Staff> { Success = false, Message = "Enter a valid passing year up to the current year." };
            if (dto.ExperienceTo.HasValue && (!dto.ExperienceFrom.HasValue || dto.ExperienceTo < dto.ExperienceFrom))
                return new ApiResponse<Staff> { Success = false, Message = "Employment end date must be on or after the start date." };
            if (dto.CertificationExpiry.HasValue && (!dto.CertificationDate.HasValue || dto.CertificationExpiry < dto.CertificationDate))
                return new ApiResponse<Staff> { Success = false, Message = "Certification expiry must be on or after the issue date." };

            var profilePictureIndex = dto.DocumentNames?
                .FindIndex(name => string.Equals(name?.Trim(), "Profile Picture", StringComparison.OrdinalIgnoreCase))
                ?? -1;
            IFormFile? profilePicture = null;
            if (profilePictureIndex >= 0)
            {
                if (dto.Files == null || dto.Files.Count <= profilePictureIndex
                    || dto.Files[profilePictureIndex] == null || dto.Files[profilePictureIndex].Length == 0)
                    return new ApiResponse<Staff> { Success = false, Message = "Selected profile picture is empty or missing." };
                profilePicture = dto.Files[profilePictureIndex];
                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedImageTypes.Contains(profilePicture.ContentType.ToLowerInvariant()))
                    return new ApiResponse<Staff> { Success = false, Message = "Profile picture must be a JPG, PNG, or WebP image." };
                if (profilePicture.Length > 5 * 1024 * 1024)
                    return new ApiResponse<Staff> { Success = false, Message = "Profile picture size cannot exceed 5 MB." };
            }

            dto.GenderCode = dto.GenderCode?.Trim().ToUpperInvariant();
            if (!GenderCodes.IsValid(dto.GenderCode))
                return new ApiResponse<Staff> { Success = false, Message = "A valid gender is required." };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // âœ… Email check in Staff table
                var emailExists = await _context.Staff
                    .AnyAsync(e => e.Email == dto.Email && e.SchoolId == dto.SchoolId);

                if (emailExists)
                {
                    return new ApiResponse<Staff>
                    {
                        Success = false,
                        Message = "Email already exists",
                        Data = null
                    };
                }

                // âœ… Generate Password
                var password = _common.GeneratePassword(dto.Name, dto.DOB);

                // âœ… Register User FIRST
                var req = new RegisterDto
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    RoleId = dto.RoleId,
                    SchoolId = dto.SchoolId,
                    Password = password
                };

                var userResult = await _user.Register(req);

                // âŒ If user registration failed
                if (userResult == null || userResult.Id <= 0)
                {
                    await transaction.RollbackAsync();

                    return new ApiResponse<Staff>
                    {
                        Success = false,
                        Message = "User registration failed",
                        Data = null
                    };
                }

                // âœ… Create Staff after User created
                var staff = new Staff
                {
                    Name = dto.Name,
                    DOB = dto.DOB,
                    GenderCode = dto.GenderCode,
                    DOJ = dto.DOJ,
                    RoleId = dto.RoleId,
                    EmploymentType = dto.EmploymentType,
                    SchoolId = dto.SchoolId,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Adress = dto.Address,
                    AddressLine2 = dto.AddressLine2,
                    Landmark = dto.Landmark,
                    City = dto.City,
                    District = dto.District,
                    State = dto.State,
                    Country = dto.Country,
                    PinCode = dto.PinCode,
                    Qualification = dto.Qualification,
                    Specialization = dto.Specialization,
                    Institute = dto.Institute,
                    University = dto.University,
                    PassingYear = dto.PassingYear,
                    Grade = dto.Grade,
                    PreviousEmployer = dto.PreviousEmployer,
                    PreviousDesignation = dto.PreviousDesignation,
                    ExperienceYears = dto.ExperienceYears,
                    ExperienceFrom = dto.ExperienceFrom,
                    ExperienceTo = dto.ExperienceTo,
                    ExperienceDetails = dto.ExperienceDetails,
                    AdditionalDetails = dto.AdditionalDetails,
                    CertificationName = dto.CertificationName,
                    CertificationIssuer = dto.CertificationIssuer,
                    CertificationNumber = dto.CertificationNumber,
                    CertificationDate = dto.CertificationDate,
                    CertificationExpiry = dto.CertificationExpiry,
                    usersid = userResult.Id, // âœ… Save UserId
                    IsActive = true,
                    Created_Date = DateTime.UtcNow
                };

                _context.Staff.Add(staff);

                await _context.SaveChangesAsync();

                if (profilePicture != null)
                {
                    var profileExtension = Path.GetExtension(profilePicture.FileName);
                    var profileFileName = Guid.NewGuid() + profileExtension;
                    var profileFolder = Path.Combine(_env.WebRootPath, "profilepictures", "staff", staff.Id.ToString());
                    Directory.CreateDirectory(profileFolder);
                    using (var stream = new FileStream(Path.Combine(profileFolder, profileFileName), FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    _context.ProfilePictures.Add(new ProfilePicture
                    {
                        PersonType = "Staff",
                        PersonId = staff.Id,
                        FileName = profilePicture.FileName,
                        FileUrl = $"/profilepictures/staff/{staff.Id}/{profileFileName}",
                        ContentType = profilePicture.ContentType,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    });
                    await _context.SaveChangesAsync();
                }

                var folderPath = Path.Combine(_env.WebRootPath, "staffdocs", staff.Id.ToString());

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // âœ… Save Documents
                if (dto.Files != null && dto.Files.Count > 0)
                {
                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        if (i == profilePictureIndex)
                            continue;

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

                        // âœ… Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // âœ… Save DB record
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

                // âœ… Commit only if ALL success
                var staffEmailPlaceholders = new Dictionary<string, string>
                {
                    { "Name", dto.Name },
                    { "Email", dto.Email },
                    { "Password", password }
                };
                var (staffEmailSubject, staffEmailBody) = await _emailService.GetEmailTemplateAsync("STAFF_CREDENTIALS", staffEmailPlaceholders);
                await _emailService.SendEmailAsync(dto.Email, staffEmailSubject, staffEmailBody);
                await transaction.CommitAsync();
                return new ApiResponse<Staff>
                {
                    Success = true,
                    Message = "Staff added successfully",
                    Data = staff
                };
            }
            catch (Exception ex)
            {
                // âŒ Rollback everything if ANYTHING fails
                await transaction.RollbackAsync();

                return new ApiResponse<Staff>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<string>> UpdateStaffAsync(UpdateStaffDto dto)
        {
            var employmentTypes = new[] { "Permanent", "Contract", "Part-Time", "Temporary", "Intern", "Substitute", "Probationary", "Visiting / Guest", "Consultant", "Volunteer" };
            dto.EmploymentType = employmentTypes.FirstOrDefault(type => string.Equals(type, dto.EmploymentType?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (dto.EmploymentType == null)
                return new ApiResponse<string> { Success = false, Message = "Select a valid employment type." };
            if (dto.PassingYear.HasValue && (dto.PassingYear < 1900 || dto.PassingYear > DateTime.Today.Year))
                return new ApiResponse<string> { Success = false, Message = "Enter a valid passing year up to the current year." };
            if (dto.ExperienceTo.HasValue && (!dto.ExperienceFrom.HasValue || dto.ExperienceTo < dto.ExperienceFrom))
                return new ApiResponse<string> { Success = false, Message = "Employment end date must be on or after the start date." };
            if (dto.CertificationExpiry.HasValue && (!dto.CertificationDate.HasValue || dto.CertificationExpiry < dto.CertificationDate))
                return new ApiResponse<string> { Success = false, Message = "Certification expiry must be on or after the issue date." };

            dto.GenderCode = dto.GenderCode?.Trim().ToUpperInvariant();
            if (!GenderCodes.IsValid(dto.GenderCode))
                return new ApiResponse<string> { Success = false, Message = "A valid gender is required." };

            if (dto.ProfilePicture != null)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(dto.ProfilePicture.ContentType.ToLowerInvariant()))
                    return new ApiResponse<string> { Success = false, Message = "Profile picture must be a JPG, PNG, or WebP image." };
                if (dto.ProfilePicture.Length == 0 || dto.ProfilePicture.Length > 5 * 1024 * 1024)
                    return new ApiResponse<string> { Success = false, Message = "Profile picture must be non-empty and no larger than 5 MB." };
            }

            var previousPicturePaths = new List<string>();
            string? newPicturePath = null;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == dto.Id);
                if (staff == null)
                    return new ApiResponse<string> { Success = false, Message = "Staff not found", Data = null };

                if (staff.RoleId != dto.RoleId)
                    return new ApiResponse<string> { Success = false, Message = "Use the staff promotion or demotion action to change the role." };

                staff.Name = dto.Name;
                staff.DOB = dto.DOB;
                staff.GenderCode = dto.GenderCode;
                staff.DOJ = dto.DOJ;
                staff.RoleId = dto.RoleId;
                staff.EmploymentType = dto.EmploymentType;
                staff.Email = dto.Email;
                staff.Phone = dto.Phone;
                staff.Adress = dto.Address;
                staff.AddressLine2 = dto.AddressLine2;
                staff.Landmark = dto.Landmark;
                staff.City = dto.City;
                staff.District = dto.District;
                staff.State = dto.State;
                staff.Country = dto.Country;
                staff.PinCode = dto.PinCode;
                staff.Qualification = dto.Qualification;
                staff.Specialization = dto.Specialization;
                staff.Institute = dto.Institute;
                staff.University = dto.University;
                staff.PassingYear = dto.PassingYear;
                staff.Grade = dto.Grade;
                staff.PreviousEmployer = dto.PreviousEmployer;
                staff.PreviousDesignation = dto.PreviousDesignation;
                staff.ExperienceYears = dto.ExperienceYears;
                staff.ExperienceFrom = dto.ExperienceFrom;
                staff.ExperienceTo = dto.ExperienceTo;
                staff.ExperienceDetails = dto.ExperienceDetails;
                if (dto.AdditionalDetails != null) staff.AdditionalDetails = dto.AdditionalDetails;
                staff.CertificationName = dto.CertificationName;
                staff.CertificationIssuer = dto.CertificationIssuer;
                staff.CertificationNumber = dto.CertificationNumber;
                staff.CertificationDate = dto.CertificationDate;
                staff.CertificationExpiry = dto.CertificationExpiry;
                staff.IsActive = dto.IsActive;
                staff.Modified_Date = DateTime.UtcNow;

                var folderPath = Path.Combine(_env.WebRootPath, "staffdocs", staff.Id.ToString());
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var existingDocs = await _context.StaffDocuments.Where(d => d.StaffId == staff.Id).ToListAsync();

                if (dto.Files != null && dto.Files.Count > 0)
                {
                    for (int i = 0; i < dto.Files.Count; i++)
                    {
                        var file = dto.Files[i];
                        if (file == null || file.Length == 0) continue;

                        var extension = Path.GetExtension(file.FileName);
                        var inputName = (dto.DocumentNames != null && dto.DocumentNames.Count > i) ? dto.DocumentNames[i] : Path.GetFileNameWithoutExtension(file.FileName);
                        var docName = inputName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? inputName : inputName + extension;
                        var uniqueFileName = Guid.NewGuid() + extension;
                        var filePath = Path.Combine(folderPath, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        if (dto.DocumentIds != null && dto.DocumentIds.Count > i && dto.DocumentIds[i].HasValue)
                        {
                            var existing = existingDocs.FirstOrDefault(d => d.Id == dto.DocumentIds[i].Value);
                            if (existing != null)
                            {
                                if (!string.IsNullOrEmpty(existing.FileUrl))
                                {
                                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.FileUrl.TrimStart('/'));
                                    if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
                                }
                                existing.DocumentName = docName;
                                existing.FileName = file.FileName;
                                existing.FileUrl = $"/staffdocs/{staff.Id}/{uniqueFileName}";
                            }
                        }
                        else
                        {
                            await _context.StaffDocuments.AddAsync(new StaffDocument
                            {
                                StaffId = staff.Id,
                                DocumentName = docName,
                                FileName = file.FileName,
                                FileUrl = $"/staffdocs/{staff.Id}/{uniqueFileName}",
                                CreatedDate = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                if (dto.ProfilePicture != null)
                {
                    var picture = dto.ProfilePicture;
                    var extension = picture.ContentType.ToLowerInvariant() switch
                    {
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        _ => ".jpg"
                    };
                    var fileName = Guid.NewGuid() + extension;
                    var folder = Path.Combine(_env.WebRootPath, "profilepictures", "staff", staff.Id.ToString());
                    Directory.CreateDirectory(folder);
                    newPicturePath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(newPicturePath, FileMode.CreateNew))
                        await picture.CopyToAsync(stream);

                    var existingPicture = await _context.ProfilePictures
                        .Where(p => p.PersonType == "Staff" && p.PersonId == staff.Id)
                        .OrderByDescending(p => p.IsActive)
                        .ThenByDescending(p => p.Id)
                        .FirstOrDefaultAsync();
                    if (existingPicture != null && !string.IsNullOrWhiteSpace(existingPicture.FileUrl))
                    {
                        var allowedFolder = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
                        var previousPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, existingPicture.FileUrl.TrimStart('/')));
                        if (!previousPath.StartsWith(allowedFolder, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                            throw new InvalidOperationException("Previous profile picture path is outside the person's profile folder.");
                        previousPicturePaths.Add(previousPath);
                    }
                    if (existingPicture == null)
                    {
                        existingPicture = new ProfilePicture
                        {
                            PersonType = "Staff",
                            PersonId = staff.Id,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.ProfilePictures.Add(existingPicture);
                    }
                    existingPicture.FileName = Path.GetFileName(picture.FileName);
                    existingPicture.FileUrl = $"/profilepictures/staff/{staff.Id}/{fileName}";
                    existingPicture.ContentType = picture.ContentType;
                    existingPicture.IsActive = true;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                // Only delete old files after the replacement is committed.
                newPicturePath = null;
                foreach (var previousPath in previousPicturePaths.Distinct())
                {
                    try { File.Delete(previousPath); }
                    catch (Exception cleanupError) when (cleanupError is IOException || cleanupError is UnauthorizedAccessException)
                    {
                        System.Diagnostics.Trace.TraceWarning($"Unable to delete replaced profile picture: {cleanupError.Message}");
                    }
                }
                return new ApiResponse<string> { Success = true, Message = "Staff updated successfully", Data = null };
            }
            catch
            {
                await transaction.RollbackAsync();
                // A failed update keeps the previous photo and discards the new upload.
                if (newPicturePath != null)
                {
                    try { File.Delete(newPicturePath); }
                    catch (Exception cleanupError) when (cleanupError is IOException || cleanupError is UnauthorizedAccessException)
                    {
                        System.Diagnostics.Trace.TraceWarning($"Unable to delete failed profile upload: {cleanupError.Message}");
                    }
                }
                return new ApiResponse<string> { Success = false, Message = "Failed to update staff", Data = null };
            }
        }

        public async Task<ApiResponse<List<RoleDto>>> GetRolesBySchoolIdAsync()
        {
            var roles = await _context.Roles
                .Select(r => new RoleDto { Id = r.Id, RoleName = r.RoleName })
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return new ApiResponse<List<RoleDto>> { Success = true, Message = "Roles fetched successfully", Data = roles };
        }

        public async Task<ApiResponse<string>> CreateAcademicSessionAsync(CreateSessionDto dto)
        {
            // ðŸ”¹ Validation
            if (dto.YearEnd <= dto.YearStart)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "YearEnd must be greater than YearStart"
                };
            }

            // ðŸ”¹ Prevent duplicate session
            var exists = await _context.AcademicSessions
                .AnyAsync(x => x.SchoolId == dto.SchoolId &&
                               x.Year_Start == dto.YearStart &&
                               x.Year_End == dto.YearEnd);

            if (exists)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Session already exists"
                };
            }

            // ðŸ”¹ Ensure only one active session
            if (dto.IsActive)
            {
                var activeSessions = await _context.AcademicSessions
                    .Where(x => x.SchoolId == dto.SchoolId && x.IsActive)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                }
            }

            // ðŸ”¹ Create new session
            var newSession = new AcademicSessions
            {
                SchoolId = dto.SchoolId,
                Year_Start = dto.YearStart,
                Year_End = dto.YearEnd,
                IsActive = dto.IsActive,
                Created_At = DateTime.Now,
                //IsActive = true
            };

            _context.AcademicSessions.Add(newSession);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Academic session created successfully"
            };
        }

        public async Task<ApiResponse<List<AcademicSessionDto>>> GetAcademicSessionsAsync(int schoolId)
        {
            var sessions = await _context.AcademicSessions
                .Where(x => x.SchoolId == schoolId)
                .OrderByDescending(x => x.Year_Start)
                .Select(x => new AcademicSessionDto
                {
                    Id = x.Id,
                    YearStart = x.Year_Start,
                    YearEnd = x.Year_End,
                    IsActive = x.IsActive,
                    CreatedAt = x.Created_At
                })
                .ToListAsync();

            return new ApiResponse<List<AcademicSessionDto>>
            {
                Success = true,
                Message = "Academic sessions fetched successfully",
                Data = sessions
            };
        }

        public async Task<ApiResponse<Schools>> UpdateSchoolAsync(SchoolUpdateDto dto, int userId)
        {
            var logoError = ValidateSchoolLogo(dto.Logo);
            if (logoError != null) return new ApiResponse<Schools> { Success = false, Message = logoError };
            var school = await _context.Schools
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.SuperAdminId == userId && x.IsActive);

            if (school == null)
                return new ApiResponse<Schools> { Success = false, Message = "School not found" };

            school.SchoolName = dto.SchoolName?.Trim() ?? string.Empty;
            school.Address = string.IsNullOrWhiteSpace(dto.Address) ? BuildAddress(dto) : dto.Address;
            school.Street = dto.Street;
            school.City = dto.City;
            school.PinCode = dto.PinCode;
            school.Country = dto.Country;
            school.State = dto.State;
            school.Landmark = dto.Landmark;
            school.Latitude = dto.Latitude;
            school.Longitude = dto.Longitude;
            school.Email = dto.Email?.Trim() ?? string.Empty;
            school.Phone = dto.Phone?.Trim() ?? string.Empty;
            school.Modified_Date = DateTime.Now;
            school.Updated_By = userId;

            ProfilePicture? newLogo = null;
            ProfilePicture? oldLogo = null;
            if (dto.Logo != null)
            {
                newLogo = await SaveSchoolLogoAsync(school.Id, dto.Logo);
                oldLogo = await _context.ProfilePictures
                    .Where(x => x.PersonType == "School" && x.PersonId == school.Id && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                if (oldLogo != null) oldLogo.IsActive = false;
                _context.ProfilePictures.Add(newLogo);
                school.LogoUrl = newLogo.FileUrl;
            }

            try { await _context.SaveChangesAsync(); }
            catch
            {
                if (newLogo != null)
                {
                    var failedPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), newLogo.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(failedPath)) File.Delete(failedPath);
                }
                throw;
            }
            if (oldLogo != null)
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), oldLogo.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }
            return new ApiResponse<Schools> { Success = true, Message = "School updated successfully", Data = school };
        }

        public async Task<ApiResponse<string>> UpdateAcademicSessionStatusAsync(UpdateAcademicSessionStatusDto dto)
        {
            var session = await _context.AcademicSessions
                .FirstOrDefaultAsync(x => x.Id == dto.SessionId && x.SchoolId == dto.SchoolId);

            if (session == null)
                return new ApiResponse<string> { Success = false, Message = "Academic session not found" };

            if (dto.IsActive)
            {
                var otherActiveSessions = await _context.AcademicSessions
                    .Where(x => x.SchoolId == dto.SchoolId && x.Id != dto.SessionId && x.IsActive)
                    .ToListAsync();

                foreach (var otherSession in otherActiveSessions)
                    otherSession.IsActive = false;
            }

            session.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = dto.IsActive
                    ? "Academic session activated successfully"
                    : "Academic session deactivated successfully"
            };
        }

        public async Task<List<StaffAttendanceDto>> GetStaffAttendanceBySchoolAsync(int schoolId)
        {
            var result = await (
                from attendance in _context.StaffAttendance
                join staff in _context.Staff
                on attendance.Staff_Id equals staff.Id
                where attendance.School_Id == schoolId
                      && attendance.IsActive == true
                      && staff.IsActive == true
                select new StaffAttendanceDto
                {
                    StaffName = staff.Name,
                    Email = staff.Email,
                    Phone = staff.Phone,
                    AttendanceDate = attendance.Attendance_Date,
                    Status = attendance.Status
                }
            ).ToListAsync();

            return result;
        }

        public async Task<ApiResponse<string>> UpdateParentAsync(UpdateParentDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Address) || string.IsNullOrWhiteSpace(dto.City) || string.IsNullOrWhiteSpace(dto.State) || string.IsNullOrWhiteSpace(dto.Country))
                return new() { Success = false, Message = "Complete the required parent address details." };
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.PinCode ?? "", @"^[1-9]\d{5}$"))
                return new() { Success = false, Message = "Enter a valid 6-digit parent PIN code." };
            var parent = await _context.ParentDetails.FirstOrDefaultAsync(p => p.Id == dto.Id &&
                _context.Students.Any(s => s.ParentId == p.Id && s.SchoolId == dto.SchoolId));
            if (parent == null) return new() { Success = false, Message = "Parent not found in this school." };
            var email = dto.Email.Trim();
            var oldEmail = parent.Email;
            var schoolIds = await _context.Students.Where(s => s.ParentId == parent.Id)
                .Select(s => s.SchoolId).Distinct().ToListAsync();
            // Parent logins are linked by email and school; preserve passwords while updating contact details.
            var logins = await _context.Students_Parents_Creds.Where(c => schoolIds.Contains(c.School_Id) &&
                c.RoleName == "Parent" && c.Email == oldEmail).ToListAsync();
            var loginIds = logins.Select(c => c.Id).ToList();
            if (await _context.ParentDetails.AnyAsync(p => p.Id != parent.Id && p.Email == email &&
                    _context.Students.Any(s => s.ParentId == p.Id && schoolIds.Contains(s.SchoolId))) ||
                await _context.Students_Parents_Creds.AnyAsync(c => schoolIds.Contains(c.School_Id) &&
                    c.Email == email && !loginIds.Contains(c.Id)))
                return new() { Success = false, Message = "This email is already used by another account. Please use a different email." };
            parent.Name = dto.Name.Trim();
            parent.Email = email;
            parent.PhoneNumber = dto.PhoneNumber.Trim();
            parent.Relationship = dto.Relationship.Trim();
            parent.Address = dto.Address?.Trim() ?? "";
            parent.AddressLine2 = dto.AddressLine2?.Trim();
            parent.Landmark = dto.Landmark?.Trim();
            parent.City = dto.City?.Trim();
            parent.District = dto.District?.Trim();
            parent.State = dto.State?.Trim();
            parent.Country = dto.Country?.Trim();
            parent.PinCode = dto.PinCode?.Trim();
            parent.Modified_Date = DateTime.Now;
            parent.Updated_By = userId;
            foreach (var login in logins)
            {
                login.Name = parent.Name;
                login.Email = email;
                login.Phone = parent.PhoneNumber;
            }
            await _context.SaveChangesAsync();
            return new() { Success = true, Message = "Parent details updated successfully." };
        }


        public async Task<(List<ParentListDto> Data, int TotalRecords)> GetParentsBySchoolAsync(
            int schoolId, int page, int pageSize, string? search, int? parentId = null)
        {
            var query = _context.ParentDetails
                .AsNoTracking()
                .Where(parent => _context.Students.Any(student =>
                    student.ParentId == parent.Id && student.SchoolId == schoolId));

            if (parentId.HasValue) query = query.Where(parent => parent.Id == parentId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(parent =>
                    parent.Name.Contains(term) ||
                    parent.Email.Contains(term) ||
                    parent.PhoneNumber.Contains(term));
            }

            var total = await query.CountAsync();
            var parents = await query
                .OrderBy(parent => parent.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var parentIds = parents.Select(parent => parent.Id).ToList();
            var students = await (
                from student in _context.Students.AsNoTracking()
                join enrollment in _context.StudentEnrollment.AsNoTracking()
                        .Where(item => item.IsActive && item.EnrollmentStatus == "Active")
                    on student.Id equals enrollment.StudentId into enrollmentGroup
                from enrollment in enrollmentGroup.DefaultIfEmpty()
                join schoolClass in _context.Classes.AsNoTracking()
                    on enrollment.ClassId equals schoolClass.Id into classGroup
                from schoolClass in classGroup.DefaultIfEmpty()
                join section in _context.SectionDetails.AsNoTracking()
                    on enrollment.SectionId equals section.Id into sectionGroup
                from section in sectionGroup.DefaultIfEmpty()
                where student.SchoolId == schoolId && parentIds.Contains(student.ParentId)
                select new
                {
                    student.ParentId,
                    EnrollmentDate = enrollment != null
                        ? enrollment.EnrollmentDate
                        : DateTime.MinValue,
                    Student = new ParentStudentDto
                    {
                        Id = student.Id,
                        StudentName = student.StudentName,
                        ProfilePictureUrl = _context.ProfilePictures
                            .Where(p => p.PersonType == "Student" && p.PersonId == student.Id && p.IsActive)
                            .OrderByDescending(p => p.Id)
                            .Select(p => p.FileUrl).FirstOrDefault(),
                        RollNumber = enrollment != null
                            ? (enrollment.RollNumber ?? student.Rollnumber)
                            : student.Rollnumber,
                        ClassName = schoolClass != null ? schoolClass.ClassName : null,
                        SectionName = section != null ? section.SectionName : null
                    }
                }).ToListAsync();

            var studentsByParent = students
                .GroupBy(item => item.ParentId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(item => item.Student.Id)
                        .Select(studentGroup => studentGroup
                            .OrderByDescending(item => item.EnrollmentDate)
                            .First().Student)
                        .OrderBy(student => string.IsNullOrWhiteSpace(student.RollNumber) ? 1 : 0)
                        .ThenBy(student => student.RollNumber?.Length ?? int.MaxValue)
                        .ThenBy(student => student.RollNumber)
                        .ThenBy(student => student.StudentName)
                        .ToList());

            var data = parents.Select(parent => new ParentListDto
            {
                Id = parent.Id,
                Name = parent.Name,
                Email = parent.Email,
                PhoneNumber = parent.PhoneNumber,
                Address = parent.Address,
                AddressLine2 = parent.AddressLine2,
                Landmark = parent.Landmark,
                City = parent.City,
                District = parent.District,
                State = parent.State,
                Country = parent.Country,
                PinCode = parent.PinCode,
                Relationship = parent.Relationship,
                IsActive = parent.IsActive,
                Students = studentsByParent.GetValueOrDefault(parent.Id) ?? new List<ParentStudentDto>()
            }).ToList();

            return (data, total);
        }

        public async Task<List<StaffAttendanceHistoryByDateDto>> GetAttendanceHistoryAsync(
     int schoolId,
     DateTime fromDate,
     DateTime toDate, int? staffId = null)
        {
            var result = await (
                from attendance in _context.StaffAttendance
                join staff in _context.Staff
                on attendance.Staff_Id equals staff.Id

                where attendance.School_Id == schoolId
                      && attendance.Attendance_Date.Date >= fromDate.Date
                      && attendance.Attendance_Date.Date <= toDate.Date
                      && attendance.IsActive == true
                      && (staffId.HasValue ? staff.Id == staffId.Value : staff.IsActive == true)

                orderby attendance.Attendance_Date descending

                select new StaffAttendanceHistoryByDateDto
                {
                    StaffId = staff.Id,
                    StaffName = staff.Name,
                    Email = staff.Email,
                    Phone = staff.Phone,
                    AttendanceDate = attendance.Attendance_Date,
                    Status = attendance.Status
                }

            ).ToListAsync();

            return result;
        }
    }
}
