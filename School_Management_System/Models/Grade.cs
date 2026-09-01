using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Grade
    {
        [Key]
        public int GradeId { get;set; }
        [Required]
        public int EnrollmentId {  get;set; }
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }
        [Required]
        [StringLength(100)]
        public string  AssessmentName { get; set; }
        [Required]
        [Range (0, 100)]
        public double Marks { get; set; }
        [Required]
        [StringLength(2)]
        public string LetterGrade {  get; set; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DateRecorded { get; set; } = DateTime.UtcNow;
        [Required]
        public bool IsActive { get; set; }

    }
}
