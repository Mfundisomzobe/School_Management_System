using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class Teacher
    {
        [Key] 
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        [Required]
        public string EmployeeId { get; set; }  
        [Required]
        public string Department {  get; set; }

        public string Qualification { get; set; }

        public DateTime HireDate { get; set; }= DateTime.UtcNow;
         public bool IsActive { get; set; }


        //  navigation property
        public virtual ICollection<Student> Students { get; set; }
        public virtual ICollection<Class> Classes { get; set; } 


    }
}
