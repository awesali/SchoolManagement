namespace SchoolManagement.Repository
{
    using global::SchoolManagement.Data;
    using global::SchoolManagement.DTOs;
    using global::SchoolManagement.Interfaces;
    using global::SchoolManagement.Model;
    using Microsoft.EntityFrameworkCore;

    namespace SchoolManagement.Repository
    {
        public class CommonRepository : ICommonRepository
        {
            private readonly AppDbContext _context;

            public CommonRepository(AppDbContext context)
            {
                _context = context;
            }

            public string GeneratePassword(string name, DateTime dob)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name is required");

                // 🔹 First 3 letters of name (remove spaces)
                var cleanName = name.Replace(" ", "");
                var namePart = cleanName.Length >= 3
                                ? cleanName.Substring(0, 3)
                                : cleanName;

                // 🔹 DOB format: ddMMyyyy
                var dobPart = dob.ToString("ddMMyyyy");

                // 🔹 Final password
                var password = $"{namePart}@{dobPart}";

                return password;
            }
            public async Task<List<StaffDropdownDto>> GetStaffBySchoolIdAsync(int schoolId)
            {
                return await (
                    from staff in _context.Staff
                    join role in _context.Roles on staff.RoleId equals role.Id
                    where staff.SchoolId == schoolId
                          && staff.IsActive
                          && role.IsActive
                          && role.RoleName.Trim().ToLower() == "teacher"
                    orderby staff.Name
                    select new StaffDropdownDto
                    {
                        Id = staff.Id,
                        Name = staff.Name
                    }
                ).ToListAsync();
            }

            public async Task<List<SubjectPicklistDto>> GetSubjectsBySchoolIdAsync(int schoolId)
            {
                return await _context.Subjects
                    .Where(s => s.SchoolId == schoolId && s.IsActive)
                    .Select(s => new SubjectPicklistDto
                    {
                        Id = s.Id,
                        SubjectName = s.SubjectName
                    })
                    .ToListAsync();
            }
        }
    }

}   
