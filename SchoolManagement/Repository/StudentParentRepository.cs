using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Model;
using SchoolManagement.Service;
using SchoolManagement.Interfaces;

namespace SchoolManagement.Repository
{
    public class StudentParentRepository : IStudentParentRepository
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public StudentParentRepository(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<bool> RegisterStudentParentAsync(StudentParentRegisterDto dto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Students_Parents_Creds
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

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
                    IsActive = true
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
            var user = await _context.Students_Parents_Creds
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

            if (user == null)
                throw new Exception("Invalid Email");

            bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password_Hash);

            if (!valid)
                throw new Exception("Invalid Password");

            // Update last login
            user.Last_Login = DateTime.Now;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Email, user.RoleName, user.Id);

            return token;
        }

        public async Task<Students_Parents_Creds> GetByEmailAsync(string email)
        {
            return await _context.Students_Parents_Creds
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateLastLoginAsync(int id)
        {
            try
            {
                var user = await _context.Students_Parents_Creds
                    .FirstOrDefaultAsync(u => u.Id == id);

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
