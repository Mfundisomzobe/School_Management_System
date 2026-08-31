using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels.Parent;

namespace School_Management_System.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ParentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private async Task<Parent> GetCurrentParentAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Parents
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);
        }

        private async Task SetSidebarDataAsync()
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                ViewBag.HasChildren = false;
                ViewBag.ChildCount = 0;
                return;
            }

            var childrenCount = await _context.StudentParents
                .Where(sp => sp.ParentId == parent.Id && sp.IsActive)
                .CountAsync();

            ViewBag.HasChildren = childrenCount > 0;
            ViewBag.ChildCount = childrenCount;
        }

        // ============================================
        // DASHBOARD - Returns ParentDashboardViewModel
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await SetSidebarDataAsync();

            var studentParents = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Class)
                            .ThenInclude(c => c.Course)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Grades)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Attendances)
                .Where(sp => sp.ParentId == parent.Id && sp.IsActive)
                .ToListAsync();

            var childSummaries = new List<ChildSummary>();

            foreach (var sp in studentParents)
            {
                var student = sp.Student;
                var enrollments = student.Enrollments?.ToList() ?? new List<Enrollment>();

                double totalMarks = 0;
                int gradeCount = 0;
                var recentGrades = new List<GradeSummary>();

                foreach (var enrollment in enrollments)
                {
                    var grade = enrollment.Grades?.FirstOrDefault();
                    if (grade != null)
                    {
                        totalMarks += grade.Marks;
                        gradeCount++;
                        recentGrades.Add(new GradeSummary
                        {
                            ClassName = enrollment.Class.ClassName,
                            Marks = grade.Marks,
                            LetterGrade = grade.LetterGrade,
                            AssessmentName = grade.AssessmentName
                        });
                    }
                }

                var overallGPA = gradeCount > 0 ? totalMarks / gradeCount : 0;

                var recentAttendance = new List<AttendanceSummary>();
                foreach (var enrollment in enrollments)
                {
                    if (enrollment.Attendances != null && enrollment.Attendances.Any())
                    {
                        var totalDays = enrollment.Attendances.Count;
                        var presentDays = enrollment.Attendances.Count(a =>
                            a.Status == Attendance.AttendanceStatus.Present ||
                            a.Status == Attendance.AttendanceStatus.Excused);
                        var percentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;

                        recentAttendance.Add(new AttendanceSummary
                        {
                            ClassName = enrollment.Class.ClassName,
                            TotalDays = totalDays,
                            PresentDays = presentDays,
                            Percentage = percentage,
                            Status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor")
                        });
                    }
                }

                var totalAttendanceDays = recentAttendance.Sum(a => a.TotalDays);
                var totalPresentDays = recentAttendance.Sum(a => a.PresentDays);
                var overallAttendance = totalAttendanceDays > 0 ? (double)totalPresentDays / totalAttendanceDays * 100 : 0;

                childSummaries.Add(new ChildSummary
                {
                    Student = student,
                    Relationship = sp.Relationship,
                    OverallGPA = overallGPA,
                    OverallAttendance = overallAttendance,
                    TotalClasses = enrollments.Count,
                    RecentGrades = recentGrades.Take(3).ToList(),
                    RecentAttendance = recentAttendance.Take(3).ToList()
                });
            }

            var viewModel = new ParentDashboardViewModel
            {
                Parent = parent,
                Children = studentParents,
                TotalChildren = studentParents.Count,
                ChildSummaries = childSummaries
            };

            ViewBag.ParentName = parent.User.FullName;
            ViewBag.ParentEmail = parent.User.Email;

            return View(viewModel);
        }

        // ============================================
        // MY CHILDREN - Returns List<StudentParent>
        // ============================================
        [HttpGet]
        public async Task<IActionResult> MyChildren()
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await SetSidebarDataAsync();

            // ✅ CORRECT: Return List<StudentParent>
            var studentParents = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Class)
                            .ThenInclude(c => c.Course)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Grades)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Attendances)
                .Where(sp => sp.ParentId == parent.Id && sp.IsActive)
                .ToListAsync();

            ViewBag.ParentName = parent.User.FullName;

            // ✅ Return List<StudentParent> (NOT ParentDashboardViewModel)
            return View(studentParents);
        }

        // ============================================
        // VIEW CHILD - Returns ChildDetailsViewModel
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ViewChild(int studentId)
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await SetSidebarDataAsync();

            var studentParent = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Class)
                            .ThenInclude(c => c.Course)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Grades)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Attendances)
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId && sp.ParentId == parent.Id && sp.IsActive);

            if (studentParent == null)
            {
                TempData["Error"] = "Student not found or not linked to your account.";
                return RedirectToAction(nameof(Dashboard));
            }

            var student = studentParent.Student;
            var enrollments = student.Enrollments?.ToList() ?? new List<Enrollment>();

            double totalMarks = 0;
            int gradeCount = 0;
            var grades = new List<GradeSummary>();

            foreach (var enrollment in enrollments)
            {
                var grade = enrollment.Grades?.FirstOrDefault();
                if (grade != null)
                {
                    totalMarks += grade.Marks;
                    gradeCount++;
                    grades.Add(new GradeSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        Marks = grade.Marks,
                        LetterGrade = grade.LetterGrade,
                        AssessmentName = grade.AssessmentName
                    });
                }
            }

            var overallGPA = gradeCount > 0 ? totalMarks / gradeCount : 0;

            var attendanceRecords = new List<AttendanceSummary>();
            foreach (var enrollment in enrollments)
            {
                if (enrollment.Attendances != null && enrollment.Attendances.Any())
                {
                    var totalDays = enrollment.Attendances.Count;
                    var presentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    var percentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;

                    attendanceRecords.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = totalDays,
                        PresentDays = presentDays,
                        Percentage = percentage,
                        Status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor")
                    });
                }
            }

            var totalAttendanceDays = attendanceRecords.Sum(a => a.TotalDays);
            var totalPresentDays = attendanceRecords.Sum(a => a.PresentDays);
            var overallAttendance = totalAttendanceDays > 0 ? (double)totalPresentDays / totalAttendanceDays * 100 : 0;

            var viewModel = new ChildDetailsViewModel
            {
                Student = student,
                Relationship = studentParent.Relationship,
                Enrollments = enrollments,
                OverallGPA = overallGPA,
                OverallAttendance = overallAttendance,
                Grades = grades,
                AttendanceRecords = attendanceRecords
            };

            ViewBag.ParentName = parent.User.FullName;

            return View(viewModel);
        }

        // ============================================
        // CHILD GRADES - Returns List<Enrollment>
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ChildGrades(int studentId)
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await SetSidebarDataAsync();

            var studentParent = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Class)
                            .ThenInclude(c => c.Course)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId && sp.ParentId == parent.Id && sp.IsActive);

            if (studentParent == null)
            {
                TempData["Error"] = "Student not found or not linked to your account.";
                return RedirectToAction(nameof(Dashboard));
            }

            var student = studentParent.Student;
            var enrollments = student.Enrollments?.ToList() ?? new List<Enrollment>();

            ViewBag.StudentId = studentId;
            ViewBag.StudentName = student.User.FullName;
            ViewBag.Relationship = studentParent.Relationship;

            return View(enrollments);
        }

        // ============================================
        // CHILD ATTENDANCE - Returns List<AttendanceSummary>
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ChildAttendance(int studentId)
        {
            var parent = await GetCurrentParentAsync();
            if (parent == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await SetSidebarDataAsync();

            var studentParent = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Class)
                            .ThenInclude(c => c.Course)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Enrollments)
                        .ThenInclude(e => e.Attendances)
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId && sp.ParentId == parent.Id && sp.IsActive);

            if (studentParent == null)
            {
                TempData["Error"] = "Student not found or not linked to your account.";
                return RedirectToAction(nameof(Dashboard));
            }

            var student = studentParent.Student;
            var enrollments = student.Enrollments?.ToList() ?? new List<Enrollment>();

            var attendanceData = new List<AttendanceSummary>();
            foreach (var enrollment in enrollments)
            {
                if (enrollment.Attendances != null && enrollment.Attendances.Any())
                {
                    var totalDays = enrollment.Attendances.Count;
                    var presentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    var percentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;

                    attendanceData.Add(new AttendanceSummary
                    {
                        ClassName = enrollment.Class.ClassName,
                        TotalDays = totalDays,
                        PresentDays = presentDays,
                        Percentage = percentage,
                        Status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor")
                    });
                }
            }

            var totalAttendanceDays = attendanceData.Sum(a => a.TotalDays);
            var totalPresentDays = attendanceData.Sum(a => a.PresentDays);
            var overallAttendance = totalAttendanceDays > 0 ? (double)totalPresentDays / totalAttendanceDays * 100 : 0;

            ViewBag.StudentId = studentId;
            ViewBag.StudentName = student.User.FullName;
            ViewBag.Relationship = studentParent.Relationship;
            ViewBag.OverallAttendance = overallAttendance;

            return View(attendanceData);
        }
    }
}