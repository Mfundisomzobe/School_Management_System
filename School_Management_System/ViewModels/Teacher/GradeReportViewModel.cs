namespace School_Management_System.ViewModels.Teacher
{
    public class GradeReportViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<StudentGradeReport> StudentReports { get; set; }
        public GradeSummaryStatistics Summary { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; }
        public List<AssessmentAverage> AssessmentAverages { get; set; }
    }

    public class StudentGradeReport
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public Dictionary<string, double?> AssessmentScores { get; set; }
        public double? OverallAverage { get; set; }
        public string OverallGrade { get; set; }
        public string Status { get; set; }
    }

    public class AssessmentAverage
    {
        public string AssessmentName { get; set; }
        public double Average { get; set; }
        public int StudentCount { get; set; }
    }

    public class GradeSummaryStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalAssessments { get; set; }
        public double ClassAverage { get; set; }
        public double HighestGrade { get; set; }
        public double LowestGrade { get; set; }
        public int StudentsPassing { get; set; }
        public int StudentsFailing { get; set; }
    }
}
