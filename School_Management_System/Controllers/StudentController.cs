using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels.Student;

namespace School_Management_System.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        // Helper method to get current student
        private async Task<Student> GetCurrentStudentAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get all enrollments for this student
            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .Include(e => e.Grades)
                .Include(e => e.Attendances)
                .Where(e => e.StudentId == student.Id && e.IsActive)
                .ToListAsync();

            // Calculate overall GPA
            double totalMarks = 0;
            int gradeCount = 0;
            var gradeSummary = new List<GradeSummary>();

            foreach (var enrollment in enrollments)
            {
                var grade = enrollment.Grades.FirstOrDefault();
                if (grade != null)
                {
                    totalMarks += grade.Marks;
                    gradeCount++;
                    gradeSummary.Add(new GradeSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        Marks = grade.Marks,
                        LetterGrade = grade.LetterGrade,
                        AssessmentName = grade.AssessmentName
                    });
                }
                else
                {
                    gradeSummary.Add(new GradeSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        Marks = null,
                        LetterGrade = "N/A",
                        AssessmentName = "Not graded yet"
                    });
                }
            }

            var overallGPA = gradeCount > 0 ? totalMarks / gradeCount : 0;

            // Calculate attendance summary
            var attendanceSummary = new List<AttendanceSummary>();
            foreach (var enrollment in enrollments)
            {
                if (enrollment.Attendances != null && enrollment.Attendances.Any())
                {
                    var totalDays = enrollment.Attendances.Count;
                    var presentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    var percentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;
                    var status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor");

                    attendanceSummary.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = totalDays,
                        PresentDays = presentDays,
                        Percentage = percentage,
                        Status = status
                    });
                }
                else
                {
                    attendanceSummary.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = 0,
                        PresentDays = 0,
                        Percentage = 0,
                        Status = "No records"
                    });
                }
            }

            // Calculate overall attendance
            var totalAttendanceDays = attendanceSummary.Sum(a => a.TotalDays);
            var totalPresentDays = attendanceSummary.Sum(a => a.PresentDays);
            var overallAttendance = totalAttendanceDays > 0 ? (double)totalPresentDays / totalAttendanceDays * 100 : 0;

            var viewModel = new StudentDashboardViewModel
            {
                Student = student,
                Enrollments = enrollments,
                TotalClasses = enrollments.Count,
                OverallGPA = overallGPA,
                OverallAttendance = overallAttendance,
                GradeSummary = gradeSummary,
                AttendanceSummary = attendanceSummary
            };

            ViewBag.StudentName = student.User.FullName;
            ViewBag.StudentEmail = student.User.Email;

            return View(viewModel);
        }
        // ==================== STUDENT PROFILE ====================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get student with all related data
            var fullStudent = await _context.Students
                .Include(s => s.User)
                .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Parent)
                        .ThenInclude(p => p.User)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(s => s.Id == student.Id);

            var parents = fullStudent.StudentParents?
                .Where(sp => sp.IsActive)
                .Select(sp => sp.Parent)
                .ToList() ?? new List<Parent>();

            var viewModel = new StudentProfileViewModel
            {
                Student = fullStudent,
                Parents = parents,
                Enrollments = fullStudent.Enrollments?.ToList() ?? new List<Enrollment>()
            };

            return View(viewModel);
        }

        // ==================== STUDENT CLASSES ====================

        [HttpGet]
        public async Task<IActionResult> MyClasses()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .Include(e => e.Grades)
                .Include(e => e.Attendances)
                .Where(e => e.StudentId == student.Id && e.IsActive)
                .ToListAsync();

            return View(enrollments);
        }

        // ==================== STUDENT GRADES ====================

        [HttpGet]
        public async Task<IActionResult> MyGrades()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Grades)
                .Where(e => e.StudentId == student.Id && e.IsActive)
                .ToListAsync();

            return View(enrollments);
        }
        // ==================== STUDENT ATTENDANCE ====================

        [HttpGet]
        public async Task<IActionResult> MyAttendance()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Attendances)
                .Where(e => e.StudentId == student.Id && e.IsActive)
                .ToListAsync();

            // Calculate attendance for each class
            var attendanceData = new List<AttendanceSummary>();
            foreach (var enrollment in enrollments)
            {
                if (enrollment.Attendances != null && enrollment.Attendances.Any())
                {
                    var attendanceTotalDays = enrollment.Attendances.Count;
                    var overallPresentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    var percentage = attendanceTotalDays > 0 ? (double)overallPresentDays / attendanceTotalDays * 100 : 0;

                    attendanceData.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = attendanceTotalDays,
                        PresentDays = overallPresentDays,
                        Percentage = percentage,
                        Status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor")
                    });
                }
                else
                {
                    attendanceData.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = 0,
                        PresentDays = 0,
                        Percentage = 0,
                        Status = "No records"
                    });
                }
            }

            var totalDays = attendanceData.Sum(a => a.TotalDays);
            var presentDays = attendanceData.Sum(a => a.PresentDays);
            var overallAttendance = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;

            ViewBag.OverallAttendance = overallAttendance;
            ViewBag.StudentName = student.User.FullName;

            return View(attendanceData);
        }
        // ==================== VIEW CLASS DETAILS ====================

        [HttpGet]
        public async Task<IActionResult> ClassDetails(int id)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .Include(e => e.Grades)
                .Include(e => e.Attendances)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id && e.StudentId == student.Id && e.IsActive);

            if (enrollment == null)
            {
                TempData["Error"] = "Class not found or you don't have access.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Calculate attendance percentage
            if (enrollment.Attendances != null && enrollment.Attendances.Any())
            {
                var totalDays = enrollment.Attendances.Count;
                var presentDays = enrollment.Attendances.Count(a =>
                    a.Status == Attendance.AttendanceStatus.Present ||
                    a.Status == Attendance.AttendanceStatus.Excused);
                ViewBag.AttendancePercentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;
            }
            else
            {
                ViewBag.AttendancePercentage = 0;
            }

            return View(enrollment);
        }

    }
}
