using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services.Interface;

using School_Management_System.ViewModels.Teacher;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace School_Management_System.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogger _auditLogger; // FIXED: Changed from audiLogger to auditLogger

        public TeacherController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAuditLogger auditLogger) // FIXED: Changed from audiLogger to auditLogger
        {
            _userManager = userManager;
            _context = context;
            _auditLogger = auditLogger;
        }

        // Helper method to get the current teacher
        private async Task<Teacher> GetCurrentTeacherAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);
        }

        // ==================== TEACHER DASHBOARD ====================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var classes = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Where(c => c.TeacherId == teacher.Id && c.IsActive)
                .ToListAsync();

            var totalStudents = classes.Sum(c => c.Enrollments.Count);
            var totalClasses = classes.Count;

            var viewModel = new TeacherDashboardViewModel
            {
                Teacher = teacher,
                Classes = classes,
                TotalStudents = totalStudents,
                TotalClasses = totalClasses
            };

            ViewBag.TeacherName = teacher.User.FullName;

            return View(viewModel);
        }

        // ==================== CLASS ROSTER ====================

        [HttpGet]
        public async Task<IActionResult> ClassRoster(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ClassId == id && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission to view it.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(classEntity);
        }

        // ==================== ATTENDANCE MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> MarkAttendance(int classId, DateTime? date)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Set date (default to today if not provided)
            var attendanceDate = date ?? DateTime.Today;

            // Prevent future dates
            if (attendanceDate > DateTime.Today)
            {
                TempData["Error"] = "Cannot mark attendance for future dates.";
                return RedirectToAction(nameof(Dashboard));
            }

            var viewModel = new MarkAttendanceViewModel
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName,
                Date = attendanceDate,
                Students = new List<StudentAttendanceViewModel>()
            };

            // Get existing attendance for this date - FIXED: Use Attendances, not Attendances
            var existingAttendance = await _context.Attendances
                .Include(a => a.Enrollments)
                .Where(a => a.Enrollments.ClassId == classId && a.AttendanceDate == attendanceDate)
                .ToListAsync();

            foreach (var enrollment in classEntity.Enrollments)
            {
                var existing = existingAttendance.FirstOrDefault(a => a.EnrollmentId == enrollment.EnrollmentId);

                var studentVM = new StudentAttendanceViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.User.FullName,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    Status = existing?.Status ?? Attendance.AttendanceStatus.Present,
                    PreviousStatus = existing?.Status ?? Attendance.AttendanceStatus.Present
                };

                viewModel.Students.Add(studentVM);
            }

            ViewBag.ClassId = classId;
            ViewBag.ClassName = classEntity.ClassName;

            // FIXED: Use the correct enum type
            ViewBag.AttendanceStatuses = Enum.GetValues(typeof(Attendance.AttendanceStatus))
                .Cast<Attendance.AttendanceStatus>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = GetEnumDisplayName(e)
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(MarkAttendanceViewModel model)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == model.ClassId && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Prevent future dates
            if (model.Date > DateTime.Today)
            {
                TempData["Error"] = "Cannot mark attendance for future dates.";
                return RedirectToAction(nameof(Dashboard));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var student in model.Students)
                {
                    // Check if attendance record exists
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.EnrollmentId == student.EnrollmentId && a.AttendanceDate == model.Date);

                    if (existingAttendance != null)
                    {
                        // Update existing record
                        existingAttendance.Status = student.Status;
                    }
                    else
                    {
                        // Create new record
                        var attendance = new Attendance
                        {
                            EnrollmentId = student.EnrollmentId,
                            AttendanceDate = model.Date,
                            Status = student.Status,
                            IsActive = true
                        };
                        await _context.Attendances.AddAsync(attendance);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Mark Attendance",
                    adminUser.FullName,
                    $"Attendance marked for class '{classEntity.ClassName}' on {model.Date:yyyy-MM-dd} by {adminUser.FullName}"
                );

                TempData["Success"] = $"Attendance for {model.Date:yyyy-MM-dd} saved successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Error saving attendance: {ex.Message}";
                return RedirectToAction(nameof(MarkAttendance), new { classId = model.ClassId, date = model.Date });
            }
        }

        // ==================== GRADE MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> EnterGrades(int classId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            var viewModel = new EnterGradesViewModel
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName,
                Students = new List<StudentGradeViewModel>()
            };

            foreach (var enrollment in classEntity.Enrollments)
            {
                var grade = enrollment.Grades.FirstOrDefault();

                var studentVM = new StudentGradeViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.User.FullName,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    Marks = grade?.Marks,
                    LetterGrade = grade?.LetterGrade,
                    GradeId = grade?.GradeId,
                    AssessmentName = grade?.AssessmentName ?? "Term Average"
                };

                viewModel.Students.Add(studentVM);
            }

            ViewBag.ClassId = classId;
            ViewBag.ClassName = classEntity.ClassName;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterGrades(EnterGradesViewModel model)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == model.ClassId && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var student in model.Students)
                {
                    // Skip if marks are null (no grade entered)
                    if (!student.Marks.HasValue)
                        continue;

                    var letterGrade = CalculateLetterGrade(student.Marks.Value);

                    if (student.GradeId.HasValue)
                    {
                        // Update existing grade
                        var existingGrade = await _context.Grades
                            .FirstOrDefaultAsync(g => g.GradeId == student.GradeId.Value);

                        if (existingGrade != null)
                        {
                            existingGrade.Marks = student.Marks.Value;
                            existingGrade.LetterGrade = letterGrade;
                            existingGrade.DateRecorded = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Create new grade
                        var grade = new Grade
                        {
                            EnrollmentId = student.EnrollmentId,
                            AssessmentName = student.AssessmentName ?? "Term Average",
                            Marks = student.Marks.Value,
                            LetterGrade = letterGrade,
                            DateRecorded = DateTime.UtcNow
                        };
                        await _context.Grades.AddAsync(grade);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Enter Grades",
                    adminUser.FullName,
                    $"Grades entered for class '{classEntity.ClassName}' by {adminUser.FullName}"
                );

                TempData["Success"] = "Grades saved successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Error saving grades: {ex.Message}";
                return RedirectToAction(nameof(EnterGrades), new { classId = model.ClassId });
            }
        }

        // ==================== VIEW INDIVIDUAL STUDENT (For Teacher) ====================

        [HttpGet]
        public async Task<IActionResult> ViewStudent(int studentId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Verify this student is in one of the teacher's classes
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Attendances) // FIXED: Use Attendances, not AttendanceRecords
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(s => s.Id == studentId &&
                    s.Enrollments.Any(e => e.Class.TeacherId == teacher.Id));

            if (student == null)
            {
                TempData["Error"] = "Student not found or not in your classes.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Calculate attendance percentage for each class
            foreach (var enrollment in student.Enrollments)
            {
                if (enrollment.Attendances.Any())
                {
                    var totalDays = enrollment.Attendances.Count;
                    var presentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    var attendancePercentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;

                    // Store as a property or use ViewBag
                    ViewBag.AttendancePercentage = attendancePercentage;
                }
            }

            return View(student);
        }

        // ==================== HELPER METHODS ====================

        private string CalculateLetterGrade(double marks)
        {
            if (marks >= 90) return "A";
            if (marks >= 80) return "B";
            if (marks >= 70) return "C";
            if (marks >= 60) return "D";
            return "F";
        }

        private string GetEnumDisplayName(Enum enumValue)
        {
            var displayAttribute = enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}