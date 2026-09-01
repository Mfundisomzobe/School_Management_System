using School_Management_System.Models;

namespace School_Management_System.ViewModels.Teacher
{
    public class AttendanceHistoryViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; }
        public Dictionary<DateTime, int> DailyAttendanceCount { get; set; }
        public int TotalDays { get; set; }
        public double OverallAttendanceRate { get; set; }
    }

    public class AttendanceRecord
    {
        public DateTime Date { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }
        public Attendance.AttendanceStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public string StatusColor { get; set; }
    }
}
