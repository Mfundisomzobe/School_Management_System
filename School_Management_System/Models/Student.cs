using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string UserId {  get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [MaxLength(20)]
        public string AdmissionNumber { get; set; }
        [Required]
        [MaxLength(50)]
        public Grade Class {  get; set; }
        public SectionLetter Section { get; set; }
        public string ParentCodeHash { get;set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsParentLinked { get; set; } = false;
        
        //Navigation Properties
        public virtual ICollection<StudentParent> StudentParents { get; set; }

        public enum SectionLetter
        {
            [Display(Name = "A")]
            A,
            [Display(Name = "B")]
            B,
            [Display(Name = "C")]
            C
        }

        public enum Grade
        {
            [Display(Name ="Grade 8")]
            Grade8,
            [Display(Name = "Grade 9")]
            Grade9,
            [Display(Name = "Grade 10")]
            Grade10,
            [Display(Name = "Grade 11")]
            Grade11,
            [Display(Name = "Grade 12")]
            Grade12


        }
        
    }
}
