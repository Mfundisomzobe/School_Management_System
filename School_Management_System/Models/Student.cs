using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId {  get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [MaxLength(20)]
        public string AdmissionNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string Class {  get; set; }

        [MaxLength(10)]
        public string Section { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; }

        
        public bool IsActive { get; set; } = true;
        
        //Navigation Property for Parents

        public virtual ICollection<StudentParent> StudentParents { get; set; }

        
        
    }
}
