using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get;set; }
        public RoleType Role {  get; set; }
        public int? SchoolId { get; set; }
        [ForeignKey(nameof(SchoolId))]
        public virtual School School { get; set; }
        public UserStatus DelectionStatus { get; set; }
        public DateTime CraeteAt { get;set; }= DateTime.UtcNow;

        public DateTime? LastLoginDate {  get; set; }
        public bool MustChangePassword { get; set; } = false;

        //Navigation Properties
        public virtual TeacherProfile TeacherProfile { get; set; }
        public virtual Student StudentProfile { get; set; }
        public virtual ParentProfile ParentProfile { get; set; }


        

        public enum RoleType
        {
            Admin,
            Teacher,
            Student,
            Parent
        }

        public enum UserStatus
        {
            [Display(Name="Active")]
            Active,
            [Display(Name = "InActive")]
            InActive
        }

    }
}
