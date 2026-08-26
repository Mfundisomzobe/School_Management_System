using School_Management_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.ViewModels
{
    public class AuditLogViewModel
    {

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Action cannot exceed 200 characters.")]
        public string Action { get; set; }

        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }


        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters.")]
        public string Details { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}

