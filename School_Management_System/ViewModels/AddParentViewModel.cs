using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class AddParentViewModel
    {
        [Required]
        [Display(Name ="Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name ="Email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name ="Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Occupation")]
        public string Occupation { get; set; }

        [Required]
        [Display(Name = "Relationship")]
        public string Relationship { get; set; } // Father, Mother, Guardian

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

    }
}
