using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }
        [Required]
        [StringLength(20)]
        public string CourseCode {  get; set; }
        [StringLength(500)]
        public string CourseDescription { get; set; }
        [Required]
        public bool IsActive { get; set; }=true;

        // Navigation Properties
        public virtual ICollection<Class> Classes { get; set; }
    }
}
