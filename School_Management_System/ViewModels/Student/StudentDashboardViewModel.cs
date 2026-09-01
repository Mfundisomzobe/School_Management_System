using School_Management_System.Models;

namespace School_Management_System.ViewModels.Student
{
    public class StudentDashboardViewModel
    {
        public Models.Student Student { get; set; }
        public List<Enrollment> Enrollments { get; set; }
        public int TotalClasses { get; set; }
        public double OverallGPA { get; set; }
        public double OverallAttendance { get; set; }
        public List<GradeSummary> GradeSummary { get; set; }
        public List<AttendanceSummary> AttendanceSummary { get; set; }
    }

    public class GradeSummary
    {
        public string ClassName { get; set; }
        public double? Marks { get; set; }
        public string LetterGrade { get; set; }
        public string AssessmentName { get; set; }
    }

    public class AttendanceSummary
    {
        public string ClassName { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public double Percentage { get; set; }
        public string Status { get; set; }
    }
}
