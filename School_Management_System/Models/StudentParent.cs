using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class StudentParent
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }
        [Required]
        public int ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }

        [Required]
        public string Relationship { get; set; }

        public bool IsPrimaryContact { get; set; }
        public bool IsActive { get; set; } = true;

    }
       
}
