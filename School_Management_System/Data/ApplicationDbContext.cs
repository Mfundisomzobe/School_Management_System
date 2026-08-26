using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace School_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<StudentParent> StudentParents { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<SchoolInfo> SchoolInfos { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Class>Classes { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Course> courses { get; set; }
        public DbSet<Enrollment> enrollments { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<Teacher>()
                .HasIndex(t => t.EmployeeId)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.AdmissionNumber)
                .IsUnique();

            
            modelBuilder.Entity<Parent>()
                .HasIndex(p => p.PhoneNumber)
                .IsUnique();


            

            //Configure relationships with ApplicationUser

            // Teacher - ApplicationUser (One-to-One)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student - ApplicationUser (One-to-One)
            modelBuilder.Entity<Student>()
                .HasOne(t => t.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Parent>()
                .HasOne(p => p.User)
                .WithOne(u => u.Parent)
                .HasForeignKey<Parent>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== STUDENT - TEACHER RELATIONSHIP =====
            // Remove cascade delete to avoid multiple cascade paths
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Students) // Add this navigation property
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===== STUDENT - PARENT(MANY - TO - MANY) =====
            // StudentParent Junction Table
            modelBuilder.Entity<StudentParent>()
                .HasOne(sp => sp.Student)
                .WithMany(s => s.StudentParents)
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentParent>()
               .HasOne(sp => sp.Parent)
               .WithMany(p => p.StudentParents)
               .HasForeignKey(sp => sp.ParentId)
               .OnDelete(DeleteBehavior.NoAction);

            // A student can have only one Father, Mother, or Guardian
            modelBuilder.Entity<StudentParent>()
                .HasIndex(sp => new { sp.StudentId, sp.Relationship })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            // ===== AUDITLOG CONFIGURATION =====
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Details).HasMaxLength(500);
             

                // Indexes for faster queries
                entity.HasIndex(e => e.ActionDate);
              
                entity.HasIndex(e => e.Action);

              
            });


            // Seed data for HospitalInfo
            modelBuilder.Entity<SchoolInfo>().HasData(
                new SchoolInfo
                {
                    SchoolInfoId = 1,
                    SchoolName = "Sister Joan's",
                    Address = "52 Mission Rd",
                    PhoneNumber = "+27-10-123-4567",
                    Email = "sisterjoan's@school.com",
                    WebsiteUrl ="https://www.sisterjoan's.com"
                }
            );
        }

    }
}
