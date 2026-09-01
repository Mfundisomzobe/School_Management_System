using School_Management_System.Models;

namespace School_Management_System.ViewModels.Parent
{
    public class ParentDashboardViewModel
    {
        public Models.Parent Parent { get; set; }
        public List<StudentParent> Children { get; set; }
        public int TotalChildren { get; set; }
        public List<ChildSummary> ChildSummaries { get; set; }
    }

    public class ChildSummary
    {
        public Models.Student Student { get; set; }
        public string Relationship { get; set; }
        public double OverallGPA { get; set; }
        public double OverallAttendance { get; set; }
        public int TotalClasses { get; set; }
        public List<GradeSummary> RecentGrades { get; set; }
        public List<AttendanceSummary> RecentAttendance { get; set; }
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
