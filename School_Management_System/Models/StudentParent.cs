using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models
{
    public class StudentParent
    {
        public int StudentParentId { get; set; }
        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public int ParentId { get; set; }

        public virtual ParentProfile Parent { get; set; }
        public ParentRelationship Relationship { get; set; }
        public bool IsPrimaryContact {  get; set; }=false;

        public enum ParentRelationship
        {
            [Display(Name ="Father")]
            Father,
            [Display(Name = "Mother")]
            Mother,
            [Display(Name = "Guardian")]
            Guardian
        }

    }
}
