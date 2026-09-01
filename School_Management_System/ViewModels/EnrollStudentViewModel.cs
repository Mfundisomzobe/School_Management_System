using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class EnrollStudentViewModel
    {
        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; }
    }
}
