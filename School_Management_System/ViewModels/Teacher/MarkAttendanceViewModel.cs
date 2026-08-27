using School_Management_System.Models;
using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels.Teacher
{
    public class MarkAttendanceViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        public DateTime Date { get; set; } = DateTime.Today;
        
        public List<StudentAttendanceViewModel> Students { get; set; }
        public int TotalStudents { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNumber { get; set; }

        [Required]
        public Attendance.AttendanceStatus Status { get; set; } = Attendance.AttendanceStatus.Present;

        public Attendance.AttendanceStatus PreviousStatus { get; set; } = Attendance.AttendanceStatus.Present;
        public bool HasAttendanceRecord { get; set; }
    }
}