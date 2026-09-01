using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }
        [Required]
        public int EnrollmentId { get;set; }
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollments {  get; set; }
        [Required]
        public DateTime AttendanceDate { get; set; }
        [Required]
        public AttendanceStatus Status { get; set; }
        [Required]
        public bool IsActive { get; set; }
   
       public enum AttendanceStatus
        {
            [Display(Name ="Present")]
            Present,
            [Display(Name = "Absent")]
            Absent,
            [Display(Name = "Late")]
            Late,
            [Display(Name = "Excused")]
            Excused
        }

    }
}
