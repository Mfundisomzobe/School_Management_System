using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class EditClassViewModel
    {
        public int ClassId { get; set; }

        [Required]
        [Display(Name = "Class Name")]
        [StringLength(100)]
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

        public bool IsActive { get; set; } = true;
    }
}
