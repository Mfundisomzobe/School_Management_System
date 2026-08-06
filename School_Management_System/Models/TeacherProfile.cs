using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class TeacherProfile
    {
        public int TeacherProfileId { get; set; } 

        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }
        public string EmployeeId { get ; set; }
        public string Department {  get; set; }
        public string Qualification {  get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
    }
}
