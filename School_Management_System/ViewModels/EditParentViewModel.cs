using School_Management_System.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.ViewModels
{
    public class EditParentViewModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name ="Full Name")]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name ="Email Address")]
        public string Email {  get; set; }
        public int StudentId { get; set; }

        [Required]
        [Phone]
        [Display(Name ="Phone Number")]
        public string PhoneNumber { get; set; }
        [Display(Name = "Address")]
        public string Address { get; set; }
        [Display(Name = "Occupation")]
        public string Occupation { get; set; }
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
}
