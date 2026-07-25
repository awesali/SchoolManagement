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
        public DbSet<ErpModule> ErpModules { get; set; }
        public DbSet<ErpPage> ErpPages { get; set; }
        public DbSet<ErpAction> ErpActions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<EmployeePermission> EmployeePermissions { get; set; }
        public DbSet<PermissionOverride> PermissionOverrides { get; set; }
        public DbSet<FeatureFlag> FeatureFlags { get; set; }
        public DbSet<PermissionAuditLog> PermissionAuditLogs { get; set; }
        public DbSet<UserSchoolAccess> UserSchoolAccess { get; set; }
        public DbSet<UserSessionAccess> UserSessionAccess { get; set; }
        public DbSet<Schools> Schools { get; set; }
        public DbSet<Staff> Staff { get; set; }

        public DbSet<Students> Students { get; set; }

        public DbSet<StaffAttendance> StaffAttendance { get; set; }

        public DbSet<StudentAttendance> StudentAttendance { get; set; }
        public DbSet<Classes> Classes { get; set; }
        public DbSet<StudentEnrollment> StudentEnrollment { get; set; }
        public DbSet<StudentPromotion> StudentPromotions { get; set; }
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
        public DbSet<ExamTypes> ExamTypes { get; set; }
        public DbSet<ExamInvigilators> ExamInvigilators { get; set; }
        public DbSet<ExamSchedules> ExamSchedules { get; set; }
        public DbSet<Exams> Exams { get; set; }
        public DbSet<FeePayments> FeePayments { get; set; }
        public DbSet<StudentFee> StudentFees { get; set; }
        public DbSet<FeeType> FeeTypes { get; set; }
        public DbSet<ExamSubjects> ExamSubjects { get; set; }
        public DbSet<SectionSubjectTeachers> SectionSubjectTeachers { get; set; }
        public DbSet<ExamMarks> ExamMarks { get; set; }
        public DbSet<ExamResults> ExamResults { get; set; }
        public DbSet<StaffSalaryStructure> StaffSalaryStructure { get; set; }
        public DbSet<SalaryPayment> SalaryPayment { get; set; }
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<TransportVehicle> TransportVehicles { get; set; }
        public DbSet<TransportDriver> TransportDrivers { get; set; }
        public DbSet<TransportConductor> TransportConductors { get; set; }
        public DbSet<TransportRoute> TransportRoutes { get; set; }
        public DbSet<TransportRouteStop> TransportRouteStops { get; set; }
        public DbSet<TransportVehicleAssignment> TransportVehicleAssignments { get; set; }
        public DbSet<StudentTransportAllocation> StudentTransportAllocations { get; set; }
        public DbSet<TransportFee> TransportFees { get; set; }
        public DbSet<TransportFeePayment> TransportFeePayments { get; set; }
        public DbSet<TransportFuelLog> TransportFuelLogs { get; set; }
        public DbSet<TransportVehicleMaintenance> TransportVehicleMaintenance { get; set; }
        public DbSet<TransportDriverAttendance> TransportDriverAttendance { get; set; }
        public DbSet<TransportGpsLocation> TransportGpsLocations { get; set; }
        public DbSet<InventoryCategory> InventoryCategories { get; set; }
        public DbSet<InventoryVendor> InventoryVendors { get; set; }
        public DbSet<InventoryProduct> InventoryProducts { get; set; }
        public DbSet<InventoryProductVariant> InventoryProductVariants { get; set; }
        public DbSet<InventoryBook> InventoryBooks { get; set; }
        public DbSet<InventoryKit> InventoryKits { get; set; }
        public DbSet<InventoryKitItem> InventoryKitItems { get; set; }
        public DbSet<InventoryPurchaseOrder> InventoryPurchaseOrders { get; set; }
        public DbSet<InventoryPurchaseOrderItem> InventoryPurchaseOrderItems { get; set; }
        public DbSet<InventoryStockTransaction> InventoryStockTransactions { get; set; }
        public DbSet<InventoryStudentOrder> InventoryStudentOrders { get; set; }
        public DbSet<InventoryStudentOrderItem> InventoryStudentOrderItems { get; set; }
        public DbSet<InventoryPayment> InventoryPayments { get; set; }
        public DbSet<InventoryReturn> InventoryReturns { get; set; }
        public DbSet<InventoryReturnItem> InventoryReturnItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Students_Parents_Creds table name
            modelBuilder.Entity<Students_Parents_Creds>()
                .ToTable("Students_Parents_Creds");

            modelBuilder.Entity<Schools>()
                .Property(s => s.Latitude)
                .HasColumnType("decimal(9,6)");

            modelBuilder.Entity<Schools>()
                .Property(s => s.Longitude)
                .HasColumnType("decimal(9,6)");

            modelBuilder.Entity<TransportGpsLocation>().Property(x => x.Latitude).HasColumnType("decimal(9,6)");
            modelBuilder.Entity<TransportGpsLocation>().Property(x => x.Longitude).HasColumnType("decimal(9,6)");
            modelBuilder.Entity<ErpModule>().HasIndex(x => x.Key).IsUnique();
            modelBuilder.Entity<ErpPage>().HasIndex(x => x.Key).IsUnique();
            modelBuilder.Entity<ErpAction>().HasIndex(x => x.Key).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(x => x.Key).IsUnique();
            modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            modelBuilder.Entity<EmployeeRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            modelBuilder.Entity<PermissionOverride>().HasIndex(x => new { x.UserId, x.PermissionId }).IsUnique();
            modelBuilder.Entity<StudentEnrollment>().HasIndex(x => new { x.StudentId, x.SessionId }).IsUnique();
            modelBuilder.Entity<StudentEnrollment>().HasIndex(x => new { x.SchoolId, x.SessionId, x.ClassId, x.SectionId, x.IsActive });
            modelBuilder.Entity<StudentAttendance>().HasIndex(x => new { x.EnrollmentId, x.Attendance_Date }).IsUnique();
            modelBuilder.Entity<ExamMarks>().HasIndex(x => new { x.EnrollmentId, x.ExamScheduleId }).IsUnique();
            modelBuilder.Entity<ExamResults>().HasIndex(x => new { x.EnrollmentId, x.ExamId }).IsUnique();
            modelBuilder.Entity<StudentFee>().HasIndex(x => new { x.EnrollmentId, x.FeeTypeId }).IsUnique();
            modelBuilder.Entity<Students>().Property(x => x.GenderCode).HasMaxLength(1);
            modelBuilder.Entity<Students>().ToTable(t => t.HasCheckConstraint(
                "CK_Students_GenderCode", "[GenderCode] IS NULL OR [GenderCode] IN ('M','F','O','N')"));
            modelBuilder.Entity<Staff>().Property(x => x.GenderCode).HasMaxLength(1);
            modelBuilder.Entity<Staff>().ToTable(t => t.HasCheckConstraint(
                "CK_Staff_GenderCode", "[GenderCode] IS NULL OR [GenderCode] IN ('M','F','O','N')"));
        }
    }
}
