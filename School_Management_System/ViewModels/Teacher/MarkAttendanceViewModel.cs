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
        public DateTime Date { get; set; } = DateTime.Today;

        public List<StudentAttendanceViewModel> Students { get; set; }
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
    }
}