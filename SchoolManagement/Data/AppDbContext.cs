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
        public DbSet<Classes> Classes { get; set; }
        public DbSet<StudentEnrollment> StudentEnrollment { get; set; }
        public DbSet<SectionDetails> SectionDetails { get; set; }
        public DbSet<ParentDetails> ParentDetails { get; set; }
        public DbSet<AcademicSessions> AcademicSessions { get; set; }
        public DbSet<StaffDocument> StaffDocuments { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<Student_Documents> Student_Documents { get; set; }
        public DbSet<Students_Parents_Creds> Students_Parents_Creds { get; set; }
        public DbSet<Subjects> Subjects { get; set; }
        public DbSet<SubjectTeachers> SubjectTeachers { get; set; }
        public DbSet<SectionSubjects> SectionSubjects { get; set; }
        public DbSet<Timetables> Timetables { get; set; }
        public DbSet<TimetablePeriods> TimetablePeriods { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Students_Parents_Creds table name
            modelBuilder.Entity<Students_Parents_Creds>()
                .ToTable("Students_Parents_Creds");
        }
    }
}