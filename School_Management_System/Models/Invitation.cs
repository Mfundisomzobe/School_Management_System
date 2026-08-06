using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Invitation
    {
        public string InvitationId { get; set; }

        public string Email {get;set;}

        public string Role { get; set; }
        public string TokenHash { get; set;}    
        public DateTime ExpiryDate { get; set; }
        [Required]
        public bool IsUsed { get; set; } = false;

        public int? SchoolId { get; set; }

        [ForeignKey(nameof(SchoolId))]
        public virtual School School { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string InvitedByUserId { get; set; }

        [ForeignKey(nameof(InvitedByUserId))]
        public virtual ApplicationUser InvitedBy { get; set; }


    }
}
