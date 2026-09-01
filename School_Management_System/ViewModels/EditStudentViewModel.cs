using School_Management_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.ViewModels
{
    public class EditStudentViewModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name ="Full Name")]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
        [Required]
        [MaxLength(20)]
        [Display(Name = "Admission Number")]
        public string AdmissionNumber { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Class")]
        public string Class { get; set; }

        [MaxLength(10)]
        [Display(Name = "Section")]
        public string Section { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Teacher")]
        public int? TeacherId { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}
