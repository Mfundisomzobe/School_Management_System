using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        // Foreign Key to ApplicationUser
        public string? UserId { get; set; }

        // Navigation Property - FIX: Add this
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        // Store name directly for quick display
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(50)]
        public string? UserRole { get; set; }
    }
}

