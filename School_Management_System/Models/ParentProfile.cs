namespace School_Management_System.Models
{
    public class ParentProfile
    {
        public int ParentProfileId { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string Occupation { get; set; }

        public string Address { get; set; }

        //Navigation Properties
        public virtual ICollection<StudentParent> StudentParents { get; set; }
    }
}
