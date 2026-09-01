using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Class
    {
        [Key]
        public int ClassId { get; set; }
        [Required]
        [StringLength(100)]
        public string ClassName { get; set; }
        [Required]
        public int CourseId { get;set; }
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; }
        [Required]
        public int Capacity { get; set; }
        [Required]
        public bool IsActive { get; set; } = true;

        //Navigation Property
        public virtual ICollection<Enrollment> Enrollments { get; set; }




    }
}
