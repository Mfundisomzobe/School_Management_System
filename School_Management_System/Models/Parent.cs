using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Parent
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }
        public string  Occupation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }

        //Navigation Property
        public virtual ICollection<StudentParent> StudentParents { get; set; }
    }
}
