namespace School_Management_System.Models
{
    public class School
    {
        public int ShoolId { get;set; }
        public string SchoolName { get;set; }

        public string Subdomain { get;set; }
        public string LogoUrl {  get;set; }
        public bool IsActive { get;set; }  
        public DateTime CreateAt { get;set; }= DateTime.UtcNow;

        //Navigation Properties
        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<Invitation>Invitations { get; set; }
    }
}
