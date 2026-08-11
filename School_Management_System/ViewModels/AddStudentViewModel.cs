using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class AddStudentViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Admission Number")]
        public string AdmissionNumber { get; set; }

        [Required]
        [Display(Name = "Class")]
        public string Class { get; set; }

        [Display(Name = "Section")]
        public string Section { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Assign Teacher")]
        public int? TeacherId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
