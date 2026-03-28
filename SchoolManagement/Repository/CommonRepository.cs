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
        }
    }

}
