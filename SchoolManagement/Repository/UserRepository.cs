// Backend section: database queries and persistence.
using System;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;

namespace SchoolManagement.Repository
{
    // Reads and updates user data.
    public class UserRepository : IUserRepository
    {
        // Dependencies and state used by this component.
        private readonly AppDbContext _context;
        private readonly IJwtService _jwt;

        // Creates the component with its required dependencies.
        public UserRepository(AppDbContext context, IJwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        // Repository operations for querying and updating stored data.
        public async Task<Users> Register(RegisterDto dto)
        {
            var exist = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (exist != null)
                throw new Exception("User already exists");

            var user = new Users
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Password_Hash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId,
                School_Id = dto.SchoolId,
                Created_At = DateTime.Now,
                Status = true,
                IsActive = true,
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string> Login(LoginDto dto)
        {
            var email = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(dto.Password))
                throw new Exception("Email and password are required");

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
                throw new Exception("Invalid Email");

            if (!user.IsActive || !user.Status)
                throw new Exception("User account is inactive");

            bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password_Hash);

            if (!valid)
                throw new Exception("Invalid Password");

            var normalizedEmail = email.ToLower();
            var teacherStaff = await (
                from staff in _context.Staff
                join role in _context.Roles on staff.RoleId equals role.Id
                where
                    (staff.usersid == user.Id || staff.Email.ToLower() == normalizedEmail)
                    && role.IsActive
                    && role.RoleName.Trim().ToLower() == "teacher"
                select new { staff.DOJ, staff.IsActive }
            ).FirstOrDefaultAsync();

            if (teacherStaff != null && !teacherStaff.IsActive)
                throw new Exception("Teacher account is inactive.");

            if (teacherStaff != null && teacherStaff.DOJ.Date > DateTime.Today)
            {
                throw new Exception(
                    $"You cannot login before your joining date ({teacherStaff.DOJ:dd MMM yyyy})."
                );
            }

            user.Last_Login = DateTime.Now;

            await _context.SaveChangesAsync();

            return _jwt.GenerateToken(user);
        }
    }
}
