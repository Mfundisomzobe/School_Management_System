using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class ApplicationUser:IdentityUser
    {
        [Required]
        [MaxLength (100)]
        public string FullName { get;set; }

        [Required]
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public  bool IsActive { get; set; }

        
        public string? RefreshToken { get;set; }


        public DateTime? RefreshTokenExpiryTime {  get; set; }

        //Navigation Properties
        public virtual Teacher Teacher { get; set; }
        public virtual Student Student { get; set; }
        public virtual Parent  Parent { get; set; }



    }
}
