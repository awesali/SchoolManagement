using Microsoft.EntityFrameworkCore;
using SchoolManagement.Model;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;

namespace SchoolManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }

        public DbSet<Roles> Roles { get; set; }
        public DbSet<Schools> Schools { get; set; }
        public DbSet<Staff> Staff { get; set; }

        public DbSet<Students> Students { get; set; }

        public DbSet<StaffAttendance> StaffAttendance { get; set; }

        public DbSet<StudentAttendance> StudentAttendance { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}