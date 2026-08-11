using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;

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

            modelBuilder.Entity<StudentParent>()
                .HasOne(sp => sp.Student)
                .WithMany(s => s.StudentParents)
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            //Configure relationships with ApplicationUser
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
        }

    }
}
