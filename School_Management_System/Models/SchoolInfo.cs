using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models
{
    public class SchoolInfo
    {
        [Key]
        public int SchoolInfoId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Hospital Name cannot exceed 200 characters.")]
        public string SchoolName { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Phone number cannot exceed 100 characters.")]
        public string PhoneNumber { get; set; }

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters.")]
        public string WebsiteUrl { get; set; }
    }
}
