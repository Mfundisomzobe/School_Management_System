using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class EditCourseViewModel
    {
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        [StringLength(100)]
        public string CourseName { get; set; }

        [Required]
        [Display(Name = "Course Code")]
        [StringLength(20)]
        public string CourseCode { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
