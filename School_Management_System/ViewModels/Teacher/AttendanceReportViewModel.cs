namespace School_Management_System.ViewModels.Teacher
{
    public class AttendanceReportViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<StudentAttendanceReport> StudentReports { get; set; }
        public SummaryStatistics Summary { get; set; }
    }

    public class StudentAttendanceReport
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int ExcusedDays { get; set; }
        public double AttendancePercentage { get; set; }
        public string Status { get; set; }
    }

    public class SummaryStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalDays { get; set; }
        public double OverallAttendance { get; set; }
        public int StudentsAbove80 { get; set; }
        public int StudentsBelow60 { get; set; }
    }
}
