// Backend section: database queries and persistence.
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;

namespace SchoolManagement.Repository
{
    // Reads and updates student parent data.
    public class StudentParentRepository : IStudentParentRepository
    {
        // Dependencies and state used by this component.
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        // Creates the component with its required dependencies.
        public StudentParentRepository(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // Repository operations for querying and updating stored data.
        public async Task<bool> RegisterStudentParentAsync(StudentParentRegisterDto dto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Students_Parents_Creds.FirstOrDefaultAsync(u =>
                    u.Email == dto.Email
                );

                if (existingUser != null)
                    return false;

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                // Create new student/parent credential
                var user = new Students_Parents_Creds
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Password_Hash = passwordHash,
                    RoleName = dto.RoleName,
                    School_Id = dto.School_Id,
                    Status = dto.Status ?? "Active",
                    Created_At = DateTime.Now,
                    IsActive = true,
                };

                _context.Students_Parents_Creds.Add(user);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> LoginStudentParentAsync(StudentParentLoginDto dto)
        {
            var email = dto.Email?.Trim();
            var user = await _context.Students_Parents_Creds.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new Exception("Invalid Email");

            bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password_Hash);

            if (!valid)
                throw new Exception("Invalid Password");

            if (user.Status == "PendingVerification")
                throw new Exception("Please verify your email address before signing in.");
            if (!user.IsActive || user.Status != "Active")
                throw new Exception("You are temporarily blocked. Please contact your school.");
            if (user.RoleName == "Student")
            {
                var activeStudentCount = await _context.Students.AsNoTracking().CountAsync(student =>
                    student.SchoolId == user.School_Id && student.Email == user.Email && student.IsActive);
                if (activeStudentCount != 1)
                    throw new Exception("You are temporarily blocked. Please contact your school.");
            }

            // Update last login
            user.Last_Login = DateTime.Now;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Email, user.RoleName, user.Id);

            return token;
        }

        public async Task<Students_Parents_Creds> GetByEmailAsync(string email)
        {
            return await _context.Students_Parents_Creds.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateLastLoginAsync(int id)
        {
            try
            {
                var user = await _context.Students_Parents_Creds.FirstOrDefaultAsync(u =>
                    u.Id == id
                );

                if (user == null)
                    return false;

                user.Last_Login = DateTime.Now;
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
