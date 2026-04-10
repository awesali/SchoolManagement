using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.DTOs;
using SchoolManagement.Interfaces;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System;

namespace SchoolManagement.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwt;

        public UserRepository(AppDbContext context, IJwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<Users> Register(RegisterDto dto)
        {
            var exist = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

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
                IsActive = true
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                throw new Exception("Invalid Email");

            bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password_Hash);

            if (!valid)
                throw new Exception("Invalid Password");

            user.Last_Login = DateTime.Now;

            await _context.SaveChangesAsync();

            return _jwt.GenerateToken(user);
        }
    }
}
