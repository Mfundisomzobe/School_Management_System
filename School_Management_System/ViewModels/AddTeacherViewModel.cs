using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class AddTeacherViewModel
    {
        [Required]
        [Display(Name ="Full Name")]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name ="Email")]
        public string Email { get; set; }
        [Required]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; }
        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; }
        [Display(Name = "Department")]
        public string Qualification { get; set; }  
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

    }
}
