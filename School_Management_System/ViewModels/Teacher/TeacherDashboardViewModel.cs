using School_Management_System.Models;

namespace School_Management_System.ViewModels.Teacher
{
    public class TeacherDashboardViewModel
    {
        public Models.Teacher Teacher { get; set; }
        public List<Class> Classes { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TodayAttendance { get; set; }
        public Dictionary<string, int> ClassEnrollmentCounts { get; set; }
    }
}