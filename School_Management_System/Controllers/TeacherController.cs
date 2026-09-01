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
        private readonly IAuditLogger _auditLogger;

        public TeacherController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAuditLogger auditLogger)
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

        // ============================================
        // HELPER METHOD - SET SIDEBAR DATA
        // ============================================
        private async Task SetSidebarDataAsync(int? currentClassId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.HasClasses = false;
                ViewBag.FirstClassId = null;
                ViewBag.CurrentClassId = null;
                ViewBag.CurrentClassName = null;
                return;
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            if (teacher == null)
            {
                ViewBag.HasClasses = false;
                ViewBag.FirstClassId = null;
                ViewBag.CurrentClassId = null;
                ViewBag.CurrentClassName = null;
                return;
            }

            // Get first class
            var firstClass = await _context.Classes
                .Where(c => c.TeacherId == teacher.Id && c.IsActive)
                .OrderBy(c => c.ClassName)
                .FirstOrDefaultAsync();

            ViewBag.HasClasses = firstClass != null;
            ViewBag.FirstClassId = firstClass?.ClassId;

            // Determine current class
            Class currentClass = null;
            if (currentClassId.HasValue && currentClassId.Value > 0)
            {
                currentClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.ClassId == currentClassId.Value && c.TeacherId == teacher.Id);
            }

            if (currentClass != null)
            {
                ViewBag.CurrentClassId = currentClass.ClassId;
                ViewBag.CurrentClassName = currentClass.ClassName;
            }
            else if (firstClass != null)
            {
                ViewBag.CurrentClassId = firstClass.ClassId;
                ViewBag.CurrentClassName = firstClass.ClassName;
            }
            else
            {
                ViewBag.CurrentClassId = null;
                ViewBag.CurrentClassName = null;
            }
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

            // Set sidebar data
            await SetSidebarDataAsync();

            // Get all classes assigned to this teacher
            var classes = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Attendances)
                .Where(c => c.TeacherId == teacher.Id && c.IsActive)
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            // Calculate totals
            var totalStudents = classes.Sum(c => c.Enrollments.Count);
            var totalClasses = classes.Count;

            // Get today's attendance count
            var today = DateTime.Today;
            var todayAttendance = await _context.Attendances
                .CountAsync(a => a.Enrollments.Class.TeacherId == teacher.Id && a.AttendanceDate == today);

            // Create class enrollment dictionary for chart
            var classEnrollmentCounts = classes
                .ToDictionary(
                    c => c.ClassName + " (" + c.Course?.CourseName + ")",
                    c => c.Enrollments.Count
                );

            var viewModel = new TeacherDashboardViewModel
            {
                Teacher = teacher,
                Classes = classes,
                TotalStudents = totalStudents,
                TotalClasses = totalClasses
            };

            ViewBag.TeacherName = teacher.User.FullName;
            ViewBag.TeacherEmail = teacher.User.Email;

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

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(id);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Attendances)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(c => c.ClassId == id && c.TeacherId == teacher.Id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission to view it.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Calculate attendance percentage for each student
            foreach (var enrollment in classEntity.Enrollments)
            {
                if (enrollment.Attendances.Any())
                {
                    var totalDays = enrollment.Attendances.Count;
                    var presentDays = enrollment.Attendances.Count(a =>
                        a.Status == Attendance.AttendanceStatus.Present ||
                        a.Status == Attendance.AttendanceStatus.Excused);
                    enrollment.AttendancePercentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;
                }
            }

            ViewBag.EnrollmentCount = classEntity.Enrollments.Count;
            ViewBag.AvailableSpots = classEntity.Capacity - classEntity.Enrollments.Count;

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

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

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
                Students = new List<StudentAttendanceViewModel>(),
                TotalStudents = classEntity.Enrollments.Count
            };

            // Get existing attendance for this date
            var existingAttendance = await _context.Attendances
                .Where(a => a.Enrollments.ClassId == classId && a.AttendanceDate == attendanceDate)
                .ToListAsync();

            int presentCount = 0, absentCount = 0, lateCount = 0, excusedCount = 0;

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
                    PreviousStatus = existing?.Status ?? Attendance.AttendanceStatus.Present,
                    HasAttendanceRecord = existing != null
                };

                // Count statuses
                if (existing != null)
                {
                    switch (existing.Status)
                    {
                        case Attendance.AttendanceStatus.Present:
                            presentCount++;
                            break;
                        case Attendance.AttendanceStatus.Absent:
                            absentCount++;
                            break;
                        case Attendance.AttendanceStatus.Late:
                            lateCount++;
                            break;
                        case Attendance.AttendanceStatus.Excused:
                            excusedCount++;
                            break;
                    }
                }

                viewModel.Students.Add(studentVM);
            }

            viewModel.PresentCount = presentCount;
            viewModel.AbsentCount = absentCount;
            viewModel.LateCount = lateCount;
            viewModel.ExcusedCount = excusedCount;

            ViewBag.ClassId = classId;
            ViewBag.ClassName = classEntity.ClassName;
            ViewBag.TotalStudents = classEntity.Enrollments.Count;

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
                .FirstOrDefaultAsync(c => c.ClassId == model.ClassId && c.TeacherId == teacher.Id && c.IsActive);

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
                int presentCount = 0, absentCount = 0, lateCount = 0, excusedCount = 0;

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

                    // Count statuses
                    switch (student.Status)
                    {
                        case Attendance.AttendanceStatus.Present:
                            presentCount++;
                            break;
                        case Attendance.AttendanceStatus.Absent:
                            absentCount++;
                            break;
                        case Attendance.AttendanceStatus.Late:
                            lateCount++;
                            break;
                        case Attendance.AttendanceStatus.Excused:
                            excusedCount++;
                            break;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Mark Attendance",
                    adminUser.FullName,
                    $"Attendance marked for class '{classEntity.ClassName}' on {model.Date:yyyy-MM-dd}. " +
                    $"Present: {presentCount}, Absent: {absentCount}, Late: {lateCount}, Excused: {excusedCount}"
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

        // ==================== ATTENDANCE HISTORY ====================

        [HttpGet]
        public async Task<IActionResult> AttendanceHistory(int classId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Attendances)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Get all attendance records for this class
            var allAttendance = await _context.Attendances
                .Include(a => a.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Where(a => a.Enrollments.ClassId == classId && a.IsActive)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            // Group by date for daily summary
            var dailyCounts = allAttendance
                .GroupBy(a => a.AttendanceDate)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            // Calculate overall attendance rate
            var totalDays = allAttendance.Select(a => a.AttendanceDate).Distinct().Count();
            var totalRecords = allAttendance.Count;
            var presentRecords = allAttendance.Count(a =>
                a.Status == Attendance.AttendanceStatus.Present ||
                a.Status == Attendance.AttendanceStatus.Excused);
            var overallRate = totalRecords > 0 ? (double)presentRecords / totalRecords * 100 : 0;

            var records = allAttendance.Select(a => new AttendanceRecord
            {
                Date = a.AttendanceDate,
                StudentName = a.Enrollments.Student.User.FullName,
                AdmissionNumber = a.Enrollments.Student.AdmissionNumber,
                Status = a.Status,
                StatusDisplay = GetEnumDisplayName(a.Status),
                StatusColor = GetStatusColor(a.Status)
            }).ToList();

            var viewModel = new AttendanceHistoryViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                AttendanceRecords = records,
                DailyAttendanceCount = dailyCounts,
                TotalDays = totalDays,
                OverallAttendanceRate = overallRate
            };

            ViewBag.ClassName = classEntity.ClassName;

            return View(viewModel);
        }

        // ==================== ATTENDANCE REPORT ====================

        [HttpGet]
        public async Task<IActionResult> AttendanceReport(int classId, DateTime? startDate, DateTime? endDate)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Set default date range (last 30 days)
            var fromDate = startDate ?? DateTime.Today.AddDays(-30);
            var toDate = endDate ?? DateTime.Today;

            // Get all students in the class
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Attendances)
                .Where(e => e.ClassId == classId && e.IsActive)
                .ToListAsync();

            var studentReports = new List<StudentAttendanceReport>();
            int studentsAbove80 = 0, studentsBelow60 = 0;

            foreach (var enrollment in enrollments)
            {
                var attendances = enrollment.Attendances
                    .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate && a.IsActive)
                    .ToList();

                var totalDays = attendances.Count;
                var presentDays = attendances.Count(a => a.Status == Attendance.AttendanceStatus.Present);
                var absentDays = attendances.Count(a => a.Status == Attendance.AttendanceStatus.Absent);
                var lateDays = attendances.Count(a => a.Status == Attendance.AttendanceStatus.Late);
                var excusedDays = attendances.Count(a => a.Status == Attendance.AttendanceStatus.Excused);
                var percentage = totalDays > 0 ? (double)(presentDays + excusedDays) / totalDays * 100 : 0;

                var status = percentage >= 80 ? "Good" : (percentage >= 60 ? "Fair" : "Poor");

                if (percentage >= 80) studentsAbove80++;
                if (percentage < 60) studentsBelow60++;

                studentReports.Add(new StudentAttendanceReport
                {
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.User.FullName,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    TotalDays = totalDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    ExcusedDays = excusedDays,
                    AttendancePercentage = percentage,
                    Status = status
                });
            }

            var totalStudents = enrollments.Count;
            var totalDaysInRange = (toDate - fromDate).Days + 1;
            var overallAttendance = studentReports.Any() ? studentReports.Average(r => r.AttendancePercentage) : 0;

            var viewModel = new AttendanceReportViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                StartDate = fromDate,
                EndDate = toDate,
                StudentReports = studentReports.OrderByDescending(r => r.AttendancePercentage).ToList(),
                Summary = new SummaryStatistics
                {
                    TotalStudents = totalStudents,
                    TotalDays = totalDaysInRange,
                    OverallAttendance = overallAttendance,
                    StudentsAbove80 = studentsAbove80,
                    StudentsBelow60 = studentsBelow60
                }
            };

            ViewBag.ClassName = classEntity.ClassName;

            return View(viewModel);
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

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            var viewModel = new EnterGradesViewModel
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName,
                TotalStudents = classEntity.Enrollments.Count,
                Students = new List<StudentGradeViewModel>()
            };

            int gradedCount = 0;
            double totalMarks = 0;

            foreach (var enrollment in classEntity.Enrollments)
            {
                var grade = enrollment.Grades.FirstOrDefault();
                var hasGrade = grade != null;

                if (hasGrade) gradedCount++;
                if (hasGrade) totalMarks += grade.Marks;

                var studentVM = new StudentGradeViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.User.FullName,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    Marks = grade?.Marks,
                    LetterGrade = grade?.LetterGrade,
                    GradeId = grade?.GradeId,
                    AssessmentName = grade?.AssessmentName ?? "Term Average",
                    HasGrade = hasGrade,
                    DateRecorded = grade?.DateRecorded
                };

                viewModel.Students.Add(studentVM);
            }

            viewModel.GradedCount = gradedCount;
            viewModel.UngradedCount = viewModel.TotalStudents - gradedCount;
            viewModel.ClassAverage = gradedCount > 0 ? totalMarks / gradedCount : 0;

            ViewBag.ClassId = classId;
            ViewBag.ClassName = classEntity.ClassName;
            ViewBag.TotalStudents = classEntity.Enrollments.Count;

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
                .FirstOrDefaultAsync(c => c.ClassId == model.ClassId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int gradedCount = 0;
                double totalMarks = 0;

                foreach (var student in model.Students)
                {
                    if (!student.Marks.HasValue)
                        continue;

                    var letterGrade = CalculateLetterGrade(student.Marks.Value);

                    if (student.GradeId.HasValue)
                    {
                        var existingGrade = await _context.Grades
                            .FirstOrDefaultAsync(g => g.GradeId == student.GradeId.Value);

                        if (existingGrade != null)
                        {
                            existingGrade.Marks = student.Marks.Value;
                            existingGrade.LetterGrade = letterGrade;
                            existingGrade.AssessmentName = student.AssessmentName ?? "Term Average";
                            existingGrade.DateRecorded = DateTime.UtcNow;
                        }
                    }
                    else
                    {
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

                    gradedCount++;
                    totalMarks += student.Marks.Value;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                var classAvg = gradedCount > 0 ? totalMarks / gradedCount : 0;

                await _auditLogger.LogAsync(
                    "Enter Grades",
                    adminUser.FullName,
                    $"Grades entered for class '{classEntity.ClassName}'. " +
                    $"Graded: {gradedCount} students, Class Average: {classAvg:F1}%"
                );

                TempData["Success"] = $"Grades saved successfully! {gradedCount} students graded.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Error saving grades: {ex.Message}";
                return RedirectToAction(nameof(EnterGrades), new { classId = model.ClassId });
            }
        }

        // ==================== GRADE HISTORY ====================

        [HttpGet]
        public async Task<IActionResult> GradeHistory(int classId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Get all grades for this class
            var allGrades = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Where(g => g.Enrollment.ClassId == classId)
                .OrderByDescending(g => g.DateRecorded)
                .ToListAsync();

            // Get distinct assessment names
            var assessments = allGrades
                .Select(g => g.AssessmentName)
                .Distinct()
                .ToList();

            // Calculate assessment averages
            var assessmentAverages = new Dictionary<string, double>();
            foreach (var assessment in assessments)
            {
                var gradesForAssessment = allGrades.Where(g => g.AssessmentName == assessment).ToList();
                var avg = gradesForAssessment.Any() ? gradesForAssessment.Average(g => g.Marks) : 0;
                assessmentAverages[assessment] = avg;
            }

            // Build grade records
            var records = new List<GradeRecord>();
            foreach (var grade in allGrades)
            {
                records.Add(new GradeRecord
                {
                    StudentName = grade.Enrollment.Student.User.FullName,
                    AdmissionNumber = grade.Enrollment.Student.AdmissionNumber,
                    AssessmentName = grade.AssessmentName,
                    Marks = grade.Marks,
                    LetterGrade = grade.LetterGrade,
                    DateRecorded = grade.DateRecorded,
                    GradeColor = GetGradeColor(grade.LetterGrade)
                });
            }

            var overallAvg = allGrades.Any() ? allGrades.Average(g => g.Marks) : 0;
            var distribution = GetGradeDistribution(allGrades);

            var viewModel = new GradeHistoryViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                GradeRecords = records,
                Assessments = assessments,
                AssessmentAverages = assessmentAverages,
                TotalStudents = classEntity.Enrollments.Count,
                OverallClassAverage = overallAvg,
                GradeDistribution = distribution
            };

            ViewBag.ClassName = classEntity.ClassName;
            ViewBag.TotalStudents = classEntity.Enrollments.Count;

            return View(viewModel);
        }

        // ==================== GRADE REPORT ====================

        [HttpGet]
        public async Task<IActionResult> GradeReport(int classId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Set sidebar data with current class ID
            await SetSidebarDataAsync(classId);

            // Verify teacher owns this class
            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Grades)
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacher.Id && c.IsActive);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found or you don't have permission.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Get all grades
            var allGrades = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Where(g => g.Enrollment.ClassId == classId)
                .ToListAsync();

            // Get assessments
            var assessments = allGrades
                .Select(g => g.AssessmentName)
                .Distinct()
                .ToList();

            // Build student reports
            var studentReports = new List<StudentGradeReport>();
            var gradeDistribution = new Dictionary<string, int> { { "A", 0 }, { "B", 0 }, { "C", 0 }, { "D", 0 }, { "F", 0 } };
            double totalMarks = 0;
            int gradedStudents = 0;
            double highest = 0;
            double lowest = 100;

            foreach (var enrollment in classEntity.Enrollments)
            {
                var studentGrades = allGrades.Where(g => g.EnrollmentId == enrollment.EnrollmentId).ToList();
                var assessmentScores = new Dictionary<string, double?>();

                foreach (var assessment in assessments)
                {
                    var grade = studentGrades.FirstOrDefault(g => g.AssessmentName == assessment);
                    assessmentScores[assessment] = grade?.Marks;
                }

                var overallAvg = studentGrades.Any() ? (double?)studentGrades.Average(g => g.Marks) : null;
                var overallGrade = overallAvg.HasValue ? CalculateLetterGrade(overallAvg.Value) : "N/A";
                var status = overallAvg.HasValue ? (overallAvg.Value >= 60 ? "Pass" : "Fail") : "Pending";

                if (overallAvg.HasValue)
                {
                    totalMarks += overallAvg.Value;
                    gradedStudents++;
                    if (overallAvg.Value > highest) highest = overallAvg.Value;
                    if (overallAvg.Value < lowest) lowest = overallAvg.Value;

                    var letter = CalculateLetterGrade(overallAvg.Value);
                    if (gradeDistribution.ContainsKey(letter))
                        gradeDistribution[letter]++;
                }

                studentReports.Add(new StudentGradeReport
                {
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.User.FullName,
                    AdmissionNumber = enrollment.Student.AdmissionNumber,
                    AssessmentScores = assessmentScores,
                    OverallAverage = overallAvg,
                    OverallGrade = overallGrade,
                    Status = status
                });
            }

            // Calculate averages per assessment
            var assessmentAverages = new List<AssessmentAverage>();
            foreach (var assessment in assessments)
            {
                var gradesForAssessment = allGrades.Where(g => g.AssessmentName == assessment).ToList();
                var avg = gradesForAssessment.Any() ? gradesForAssessment.Average(g => g.Marks) : 0;
                assessmentAverages.Add(new AssessmentAverage
                {
                    AssessmentName = assessment,
                    Average = avg,
                    StudentCount = gradesForAssessment.Count
                });
            }

            var classAverage = gradedStudents > 0 ? totalMarks / gradedStudents : 0;
            var passingStudents = studentReports.Count(r => r.OverallAverage.HasValue && r.OverallAverage.Value >= 60);
            var failingStudents = studentReports.Count(r => r.OverallAverage.HasValue && r.OverallAverage.Value < 60);

            var viewModel = new GradeReportViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                StudentReports = studentReports,
                AssessmentAverages = assessmentAverages,
                GradeDistribution = gradeDistribution,
                Summary = new GradeSummaryStatistics
                {
                    TotalStudents = classEntity.Enrollments.Count,
                    TotalAssessments = assessments.Count,
                    ClassAverage = classAverage,
                    HighestGrade = highest,
                    LowestGrade = lowest,
                    StudentsPassing = passingStudents,
                    StudentsFailing = failingStudents
                }
            };

            ViewBag.ClassName = classEntity.ClassName;

            return View(viewModel);
        }

        // ==================== VIEW INDIVIDUAL STUDENT ====================

        [HttpGet]
        public async Task<IActionResult> ViewStudent(int studentId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Set sidebar data
            await SetSidebarDataAsync();

            // Verify this student is in one of the teacher's classes
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Attendances)
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
                    ViewBag.AttendancePercentage = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;
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

        private string GetStatusColor(Attendance.AttendanceStatus status)
        {
            return status switch
            {
                Attendance.AttendanceStatus.Present => "success",
                Attendance.AttendanceStatus.Absent => "danger",
                Attendance.AttendanceStatus.Late => "warning",
                Attendance.AttendanceStatus.Excused => "info",
                _ => "secondary"
            };
        }

        private string GetGradeColor(string letterGrade)
        {
            return letterGrade?.ToLower() switch
            {
                "a" => "success",
                "b" => "info",
                "c" => "warning",
                "d" => "orange",
                "f" => "danger",
                _ => "secondary"
            };
        }

        private string GetGradeDistribution(List<Grade> grades)
        {
            if (!grades.Any()) return "No grades";

            var total = grades.Count;
            var aCount = grades.Count(g => g.LetterGrade == "A");
            var bCount = grades.Count(g => g.LetterGrade == "B");
            var cCount = grades.Count(g => g.LetterGrade == "C");
            var dCount = grades.Count(g => g.LetterGrade == "D");
            var fCount = grades.Count(g => g.LetterGrade == "F");

            return $"A: {aCount} ({aCount * 100 / total}%), B: {bCount} ({bCount * 100 / total}%), C: {cCount} ({cCount * 100 / total}%), D: {dCount} ({dCount * 100 / total}%), F: {fCount} ({fCount * 100 / total}%)";
        }
    }
}