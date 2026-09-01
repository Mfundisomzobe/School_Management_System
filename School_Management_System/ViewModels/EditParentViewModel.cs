using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels
{
    public class EditParentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Occupation")]
        public string Occupation { get; set; }

        [Display(Name = "Student")]
        public int? StudentId { get; set; }

        [Display(Name = "Relationship")]
        public string Relationship { get; set; } = "Parent";

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}