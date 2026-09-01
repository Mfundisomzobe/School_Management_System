using School_Management_System.Models;

namespace School_Management_System.ViewModels.Parent
{
    public class ChildDetailsViewModel
    {

        public Models.Student Student { get; set; }
        public string Relationship { get; set; }
        public List<Enrollment> Enrollments { get; set; }
        public double OverallGPA { get; set; }
        public double OverallAttendance { get; set; }
        public List<GradeSummary> Grades { get; set; }
        public List<AttendanceSummary> AttendanceRecords { get; set; }
    }
}
