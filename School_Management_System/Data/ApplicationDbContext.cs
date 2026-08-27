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
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }



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

            // Course Configuration
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(c => c.CourseId);
                entity.HasIndex(c => c.CourseCode).IsUnique();
            });

            // Class Configuration
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(c => c.ClassId);
                entity.HasOne(c => c.Course)
                    .WithMany(c => c.Classes)
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Teacher)
                    .WithMany(t => t.Classes)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Enrollment Configuration
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
                entity.HasIndex(e => new { e.StudentId, e.ClassId }).IsUnique();

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Attendance Configuration
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(a => a.AttendanceId);
                entity.HasIndex(a => new { a.EnrollmentId, a.AttendanceDate }).IsUnique();

                entity.HasOne(a => a.Enrollments)
                    .WithMany(e => e.Attendances)
                    .HasForeignKey(a => a.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Grade Configuration
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(g => g.GradeId);
                entity.HasOne(g => g.Enrollment)
                    .WithMany(e => e.Grades)
                    .HasForeignKey(g => g.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Action).IsRequired().HasMaxLength(200);
                entity.Property(a => a.FullName).HasMaxLength(100);
                entity.Property(a => a.Details).HasMaxLength(500);
                entity.Property(a => a.IpAddress).HasMaxLength(50);
                entity.Property(a => a.UserRole).HasMaxLength(50);
                entity.Property(a => a.IsActive).IsRequired().HasDefaultValue(true); // ADD THIS LINE
                entity.HasIndex(a => a.ActionDate);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Action);

                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
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


            // 1. Seed Courses
            modelBuilder.Entity<Course>().HasData(
              
                new Course { CourseId = 2, CourseName = "English Literature", CourseCode = "ENG101", CourseDescription = "Introduction to English literature, poetry, and prose", IsActive = true },
                new Course { CourseId = 3, CourseName = "Physical Science", CourseCode = "SCI101", CourseDescription = "Basic physics and chemistry principles", IsActive = true },
                new Course { CourseId = 4, CourseName = "Biology", CourseCode = "SCI102", CourseDescription = "Introduction to biology and life sciences", IsActive = true },
                new Course { CourseId = 5, CourseName = "World History", CourseCode = "HIST101", CourseDescription = "Survey of world history from ancient to modern times", IsActive = true },
                new Course { CourseId = 6, CourseName = "Geography", CourseCode = "GEOG101", CourseDescription = "Physical and human geography", IsActive = true },
                new Course { CourseId = 7, CourseName = "Computer Science", CourseCode = "CS101", CourseDescription = "Introduction to programming and computer science", IsActive = true },
                new Course { CourseId = 8, CourseName = "Business Studies", CourseCode = "BUS101", CourseDescription = "Introduction to business principles and economics", IsActive = true },
                new Course { CourseId = 9, CourseName = "Art and Design", CourseCode = "ART101", CourseDescription = "Fundamentals of art and creative design", IsActive = true },
                new Course { CourseId = 10, CourseName = "Music", CourseCode = "MUS101", CourseDescription = "Introduction to music theory and appreciation", IsActive = true },
                new Course { CourseId = 11, CourseName = "Physical Education", CourseCode = "PE101", CourseDescription = "Physical fitness and sports education", IsActive = true },
                new Course { CourseId = 12, CourseName = "French Language", CourseCode = "FREN101", CourseDescription = "Introduction to French language and culture", IsActive = true },
                new Course { CourseId = 13, CourseName = "Spanish Language", CourseCode = "SPAN101", CourseDescription = "Introduction to Spanish language and culture", IsActive = true },
                new Course { CourseId = 14, CourseName = "Psychology", CourseCode = "PSYCH101", CourseDescription = "Introduction to psychology and human behavior", IsActive = true },
                new Course { CourseId = 15, CourseName = "Economics", CourseCode = "ECON101", CourseDescription = "Introduction to micro and macroeconomics", IsActive = true }
            );

            // 2. Seed Classes
            modelBuilder.Entity<Class>().HasData(
               
                new Class { ClassId = 2, ClassName = "Grade 10B - Mathematics", CourseId = 1, Capacity = 25, IsActive = true },
                new Class { ClassId = 3, ClassName = "Grade 11A - English", CourseId = 2, Capacity = 25, IsActive = true },
                new Class { ClassId = 4, ClassName = "Grade 11B - English", CourseId = 2, Capacity = 25, IsActive = true },
                new Class { ClassId = 5, ClassName = "Grade 10A - Science", CourseId = 3, Capacity = 20, IsActive = true },
                new Class { ClassId = 6, ClassName = "Grade 11A - Biology", CourseId = 4, Capacity = 20, IsActive = true },
                new Class { ClassId = 7, ClassName = "Grade 12A - History", CourseId = 5, Capacity = 30, IsActive = true },
                new Class { ClassId = 8, ClassName = "Grade 12B - History", CourseId = 5, Capacity = 30, IsActive = true },
                new Class { ClassId = 9, ClassName = "Grade 10A - Computer Science", CourseId = 7, Capacity = 20, IsActive = true },
                new Class { ClassId = 10, ClassName = "Grade 11A - Computer Science", CourseId = 7, Capacity = 20, IsActive = true },
                new Class { ClassId = 11, ClassName = "Grade 10A - Art", CourseId = 9, Capacity = 15, IsActive = true },
                new Class { ClassId = 12, ClassName = "Grade 11A - Physical Education", CourseId = 11, Capacity = 30, IsActive = true },
                new Class { ClassId = 13, ClassName = "Grade 12A - Business Studies", CourseId = 8, Capacity = 25, IsActive = true }
            );
        }

    }
}
