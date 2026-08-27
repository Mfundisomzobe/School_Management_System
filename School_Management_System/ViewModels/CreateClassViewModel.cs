using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class CreateClassViewModel
    {
        [Required]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Required]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; }

        [Required]
        [Display(Name = "Capacity")]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
        public int Capacity { get; set; } = 30;
    }
}
