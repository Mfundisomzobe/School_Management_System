using School_Management_System.Models;

namespace School_Management_System.ViewModels.Student
{
    public class StudentProfileViewModel
    {
        public Models.Student Student { get; set; }
        public List<Models.Parent> Parents { get; set; }
        public List<Enrollment> Enrollments { get; set; }
    }
}
