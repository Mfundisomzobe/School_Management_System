using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Action cannot exceed 200 characters.")]
        public string Action { get; set; }
      
        [StringLength(100)]
        public string FullName { get; set; }


        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters.")]
        public string Details { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}
