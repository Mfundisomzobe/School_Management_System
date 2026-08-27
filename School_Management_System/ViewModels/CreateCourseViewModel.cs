using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }


        [Required]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; }

        [Display(Name = "Description")]
        public string CourseDescription { get; set; }
    }
}
