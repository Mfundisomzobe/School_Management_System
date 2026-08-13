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
        }

    }
}
