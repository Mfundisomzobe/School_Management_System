
using System.ComponentModel.DataAnnotations;
namespace School_Management_System.ViewModels
{
    

   
    
        public class EditTeacherViewModel
        {
            public int Id { get; set; }

            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Employee ID")]
            public string EmployeeId { get; set; }

            [Required]
            [Display(Name = "Department")]
            public string Department { get; set; }

            [Display(Name = "Qualification")]
            public string Qualification { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; }
        }
    }
