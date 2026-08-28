using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;

using School_Management_System.Models;
using School_Management_System.Services.Implementation;
using School_Management_System.Services.Interface;
using School_Management_System.ViewModels;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;


namespace School_Management_System.Controllers
{
    [Authorize(Roles ="Admin")]
    public class AdminController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            AuditLogger auditLogger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditLogger = auditLogger;
        }


        // System Administrator Dashboard  
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalTeachers = await _context.Teachers.CountAsync(t => t.IsActive);
            ViewBag.TotalStudents = await _context.Students.CountAsync(s => s.IsActive);
            ViewBag.TotalParents = await _context.Parents.CountAsync(p => p.IsActive);
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            ViewBag.TotalClasses = await _context.Classes.CountAsync(c => c.IsActive);

            // Add these for the sidebar badges
            ViewBag.TeacherCount = ViewBag.TotalTeachers;
            ViewBag.StudentCount = ViewBag.TotalStudents;
            ViewBag.ParentCount = ViewBag.TotalParents;
            ViewBag.CourseCount = ViewBag.TotalCourses;
            ViewBag.ClassCount = ViewBag.TotalClasses;
            return View();
        }

       
        //Teacher Management
        // Synchronous version for GET (no async needed)
        private string GenerateEmployeeIdSync()
        {
            var year = DateTime.UtcNow.Year.ToString().Substring(2);
            var count = _context.Teachers.Count() + 1;
            return $"EMP-{year}-{count:D4}";
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars,6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // Helper method to generate Admission Number
        private string GenerateAdmissionNumberAsync()
        {
            var year = DateTime.UtcNow.Year.ToString().Substring(2);
            var count =  _context.Students.Count() + 1;
            return $"STU-{year}-{count:D4}";
        }
        [HttpGet]
        public IActionResult GenerateCredentials()
        {
            return Json(new
            {
                password = GenerateRandomPassword(),
                employeeId = GenerateEmployeeIdSync(),
                admissionNumber= GenerateAdmissionNumberAsync()
            });
        }


        // ==================== AUDIT LOG MANAGEMENT ====================

        // GET: View Audit Logs with Pagination
        [HttpGet]
        public async Task<IActionResult> ViewAuditLogs(int page = 1, string searchTerm = null,
            string filterAction = null, DateTime? filterDate = null, string filterUser = null)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var query = _context.AuditLogs
                .Include(l => l.User)  // Include User navigation property
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l =>
                    l.Action.Contains(searchTerm) ||
                    (l.Details != null && l.Details.Contains(searchTerm)) ||
                    (l.FullName != null && l.FullName.Contains(searchTerm)) ||
                    (l.User != null && l.User.Email.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(filterAction))
            {
                query = query.Where(l => l.Action == filterAction);
            }

            if (filterDate.HasValue)
            {
                query = query.Where(l => l.ActionDate.Date == filterDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(filterUser))
            {
                query = query.Where(l =>
                    (l.FullName != null && l.FullName.Contains(filterUser)) ||
                    (l.User != null && l.User.Email.Contains(filterUser)));
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Get paginated logs
            var logs = await query
                .OrderByDescending(l => l.ActionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get distinct actions for filter dropdown
            var actions = await _context.AuditLogs
                .Select(l => l.Action)
                .Distinct()
                .ToListAsync();

            var model = new AuditLogViewModel
            {
                Logs = logs,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                FilterAction = filterAction,
                FilterDate = filterDate,
                FilterUser = filterUser
            };

            ViewBag.Actions = new SelectList(actions);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(model);
        }

        // GET: View Audit Log Details
        [HttpGet]
        public async Task<IActionResult> AuditLogDetails(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var log = await _context.AuditLogs
                .Include(l => l.User)  // Include User navigation property
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
            {
                TempData["Error"] = "Audit log not found.";
                return RedirectToAction("ViewAuditLogs");
            }

            return View(log);
        }

        // GET: Clear Old Audit Logs (Optional)
        [HttpGet]
        public async Task<IActionResult> ClearOldLogs(int days = 30)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var oldLogs = await _context.AuditLogs
                .Where(l => l.ActionDate < cutoffDate)
                .ToListAsync();

            var count = oldLogs.Count;
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            var adminUser = await _userManager.GetUserAsync(User);
            await _auditLogger.LogAsync(
                "Clear Audit Logs",
                adminUser.FullName,
                $"Cleared {count} audit logs older than {days} days"
            );

            TempData["Success"] = $"Successfully cleared {count} audit logs older than {days} days.";
            return RedirectToAction("ViewAuditLogs");
        }

        // GET: Export Audit Logs (Optional)
        [HttpGet]
        public async Task<IActionResult> ExportAuditLogs(DateTime? fromDate, DateTime? toDate)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var query = _context.AuditLogs
                .Include(l => l.User)  // Include User navigation property
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.ActionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.ActionDate <= toDate.Value);
            }

            var logs = await query
                .OrderByDescending(l => l.ActionDate)
                .ToListAsync();

            // Create CSV export
            var csvContent = "ID,Action,User,Details,Date\n";
            foreach (var log in logs)
            {
                var userName = log.FullName ?? log.User?.FullName ?? "System";
                csvContent += $"{log.Id},{log.Action},{userName},{log.Details},{log.ActionDate}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            var fileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }





        //=======TEACHER MANAGEMENT======//





        [HttpGet]
        public IActionResult AddTeacher()
        {
            var model = new AddTeacherViewModel
            {
                // Generate Employee ID
                EmployeeId = GenerateEmployeeIdSync(),
                // Generate Password
                Password = GenerateRandomPassword()
            };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher( AddTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EmployeeId = GenerateEmployeeIdSync();
                model.Password = GenerateRandomPassword();
                // Log invalid model state
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Model Error: {error.ErrorMessage}");
                }
                return View(model);
            }
            //Check if email already exists
             var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                model.EmployeeId = GenerateEmployeeIdSync();
                model.Password = GenerateRandomPassword();
                return View(model);
            }
            // Use the generated password from the form
        var password = model.Password;
            // If somehow empty, generate a new one
            if (string.IsNullOrEmpty(password))
            {
                password = GenerateRandomPassword();
            }


            //Check if EmployeeId already Exists 
            if (await _context.Teachers.AnyAsync(t => t.EmployeeId== model.EmployeeId))
            {
                ModelState.AddModelError("EmployeeId", "Employee ID already Exists");

                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // AUTO-GENERATE EMPLOYEE ID
                var employeeId = GenerateEmployeeIdSync();

                // AUTO-GENERATE PASSWORD
                var Password = GenerateRandomPassword();
                //Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = "Teacher",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true, //Auto Confirm email
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
               if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    model.EmployeeId = GenerateEmployeeIdSync();
                    model.Password = GenerateRandomPassword();
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "Teacher");

                //Create Teacher Profile

                var teacher = new Teacher
                {
                    UserId = user.Id,
                    EmployeeId = model.EmployeeId,
                    Department = model.Department,
                    Qualification = model.Qualification,
                    HireDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.Teachers.AddAsync(teacher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Teacher {model.FullName} added successfully!";
                return RedirectToAction("AddTeacher");

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("",$"Failed to add teacher: {ex.Message}");
                model.EmployeeId = GenerateEmployeeIdSync();
                model.Password = GenerateRandomPassword();
                return View(model);
            }


        }

        [HttpGet]
        public async Task<IActionResult> ViewTeachers()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
              
                .ToListAsync();

            return View(teachers);
        }

        //EDIT: TEACHER
        // ===== EDIT TEACHER - GET =====
        [HttpGet]
        public async Task<IActionResult> EditTeacher(int? id)
        {
            if (id == null)
                return NotFound();

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            var model = new EditTeacherViewModel
            {
                Id = teacher.Id,
                FullName = teacher.User.FullName,
                Email = teacher.User.Email,
                EmployeeId = teacher.EmployeeId,
                Department = teacher.Department,
                Qualification = teacher.Qualification,
                IsActive = teacher.IsActive
            };

            return View(model);
        }

        // ===== EDIT TEACHER - POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(EditTeacherViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (teacher == null)
                    return NotFound();

                // Check duplicate email
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Id != teacher.UserId);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already used by another user.");
                    return View(model);
                }

                // Check duplicate Employee ID
                var existingTeacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.EmployeeId == model.EmployeeId && t.Id != model.Id);

                if (existingTeacher != null)
                {
                    ModelState.AddModelError("EmployeeId", "This Employee ID is already used by another teacher.");
                    return View(model);
                }
                var adminUser = await _userManager.GetUserAsync(User);
                string oldName = teacher.User.FullName;

                // Update User
                teacher.User.FullName = model.FullName;
                teacher.User.Email = model.Email;
                teacher.User.UserName = model.Email;

                // Update Teacher
                teacher.EmployeeId = model.EmployeeId;
                teacher.Department = model.Department;
                teacher.Qualification = model.Qualification;
                teacher.IsActive = model.IsActive;

                await _userManager.UpdateAsync(teacher.User);
                await _context.SaveChangesAsync();

                //// Log the edit action
                //await _auditLogger.LogWithUserAsync(
                //    "Edit Teacher",
                //    adminUser.Id,
                //    adminUser.FullName,
                //    $"Teacher '{oldName}' was updated by {adminUser.FullName}. New name: {model.FullName}"
                //);

                TempData["Success"] = $"Teacher '{model.FullName}' updated successfully!";
                return RedirectToAction("ViewTeachers");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Database error: {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }

        // ===== SOFT DELETE TEACHER =====

        [HttpGet]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if(teacher  == null)
            {
                TempData["Error"] = "Teacher not Found.";
                return RedirectToAction("ViewTeacher");
            }
            var adminUser = await _userManager.GetUserAsync(User);
            string teacherName = teacher.User.FullName;

            //Soft delete -just deactivate
            teacher.IsActive = false;
            teacher.User.IsActive = false;
            await _context.SaveChangesAsync();


            // OR Method 2: Using FullName only
             await _auditLogger.LogAsync(
                 "Delete Teacher",
                 adminUser.FullName,
                 $"Teacher '{teacherName}' was deactivated"
             );
            TempData["Success"] = $" Teacher '{teacher.User.FullName}' has been deactivated successfully.";
            return RedirectToAction("ViewTeachers");
        }

        [HttpGet]
        public async Task<IActionResult> ReactivateTeacher(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

             var teacher = await _context.Teachers
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                TempData["Error"] = "Teacher not found.";
                return RedirectToAction("ViewTeachers");
            }

            // Reactivate
            teacher.IsActive = true;
            teacher.User.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Teacher '{teacher.User.FullName}' has been reactivated successfully.";
            return RedirectToAction("ViewTeachers");

        }

















        //=======STUDENT MANAGEMENT======//




        [HttpGet]
        public async  Task<IActionResult> AddStudent()
        {

            var model = new AddStudentViewModel
            {
                // Generate Employee ID
                AdmissionNumber =GenerateAdmissionNumberAsync(),
                // Generate Password
                Password = GenerateRandomPassword()
            };

            ViewBag.Teachers = new SelectList(
                await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    Id = t.Id,
                    Name = t.User.FullName + "(" + t.EmployeeId + ")"
                })
                .ToListAsync(),
                "Id", "Name");

            return View();

        }

        


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(AddStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AdmissionNumber = GenerateAdmissionNumberAsync();
                model.Password = GenerateRandomPassword();
                ViewBag.Teachers = new SelectList(
                   await _context.Teachers
                       .Include(t => t.User)
                       .Where(t => t.IsActive)
                       .Select(t => new
                       {
                           Id = t.Id,
                           Name = t.User.FullName + " (" + t.EmployeeId + ")"
                       })
                       .ToListAsync(),
                   "Id", "Name"
               );
                return View(model);
            }
          

            //Check if email alredy exists 
            var existingUser = await _userManager.FindByEmailAsync( model.Email );

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already Exists.");
                model.AdmissionNumber = GenerateAdmissionNumberAsync();
                model.Password = GenerateRandomPassword();

                ViewBag.Teachers = new SelectList(
                   await _context.Teachers
                       .Include(t => t.User)
                       .Where(t => t.IsActive)
                       .Select(t => new
                       {
                           Id = t.Id,
                           Name = t.User.FullName + " (" + t.EmployeeId + ")"
                       })
                       .ToListAsync(),
                   "Id", "Name"
               );
                return View(model);

            }

            //Check if admission number already Exists 

            if(await _context.Students.AnyAsync(s => s.AdmissionNumber == model.AdmissionNumber))
            {
                ModelState.AddModelError("AdmissionNumber", "Admission number already exists.");
                model.AdmissionNumber = GenerateAdmissionNumberAsync();
                model.Password = GenerateRandomPassword();

                ViewBag.Teachers = new SelectList(
                  await _context.Teachers
                      .Include(t => t.User)
                      .Where(t => t.IsActive)
                      .Select(t => new
                      {
                          Id = t.Id,
                          Name = t.User.FullName + " (" + t.EmployeeId + ")"
                      })
                      .ToListAsync(),
                  "Id", "Name"
              );
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // AUTO-GENERATE ADMISSION NUMBER
                var admissionNumber =  GenerateAdmissionNumberAsync();

                // AUTO-GENERATE PASSWORD
                var password = GenerateRandomPassword();
                //Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = "Student",
                    CreatedAt = DateTime.UtcNow,
                    IsActive =true,
                    EmailConfirmed = true //Auto Confirmation
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);

                if (!createResult.Succeeded)
                {
                  foreach(var error in createResult.Errors)
                    {
                        ModelState.AddModelError("",error.Description);
                        model.AdmissionNumber = GenerateAdmissionNumberAsync();
                        model.Password = GenerateRandomPassword();
                        return View(model);
                    }

                }

                //Add Student role 
                await _userManager.AddToRoleAsync(user, "Student");

                //Create Student Profile
                var student = new Student
                {
                    UserId = user.Id,
                    AdmissionNumber = model.AdmissionNumber,
                    Class = model.Class,
                    Section = model.Section,
                    DateOfBirth = model.DateOfBirth,
                    TeacherId = model.TeacherId,
                    IsActive = true

                };
                await _context.Students.AddAsync(student);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Student {model.FullName} added successfully!";

                return RedirectToAction("AddStudent");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                 ModelState.AddModelError("", $"Failed to add student: {ex.Message}");
                model.AdmissionNumber = GenerateAdmissionNumberAsync();
                model.Password = GenerateRandomPassword();

                ViewBag.Teachers = new SelectList(
                   await _context.Teachers
                       .Include(t => t.User)
                       .Where(t => t.IsActive)
                       .Select(t => new
                       {
                           Id = t.Id,
                           Name = t.User.FullName + " (" + t.EmployeeId + ")"
                       })
                       .ToListAsync(),
                   "Id", "Name"
               );
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewStudents()
        {
            var students = await _context.Students
               .Include(t => t.User)
               .Include(s => s.Teacher)
                   .ThenInclude(t => t.User)
               .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Parent)
                       .ThenInclude(p => p.User)
              
               .ToListAsync();

            return View(students);
        }


        //EDIT: STUDENT
        // ===== EDIT STUDENT - GET =====
        [HttpGet]
        public async Task<IActionResult> EditStudent(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(t => t.User)
                .Include(s => s.Teacher)
                   .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (student == null)
                return NotFound();

            string teacherName = student.Teacher?.User?.FullName ?? "Not Assigned";
            var model = new EditStudentViewModel
            {
                Id = student.Id,
                FullName = student.User.FullName,
                Email = student.User.Email,
                Section = student.Section,
                Class = student.Class,
                AdmissionNumber = student.AdmissionNumber,
                TeacherId = student.TeacherId,
                IsActive = student.IsActive,
                DateOfBirth = student.DateOfBirth
               


            };
            

            // Set ViewBag for the view
            ViewBag.TeacherName = teacherName;  // ← IMPORTANT!
            return View(model);
        }

        // ===== EDIT STUDENT - POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(EditStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, get teacher name again for the view
                var student = await _context.Students
                    .Include(s => s.Teacher)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (student != null)
                {
                    ViewBag.TeacherName = student.Teacher?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }
            try
            {
                var student = await _context.Students
                    .Include(t => t.User)
                    .Include (s => s.Teacher)
                       .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (student == null)
                    return NotFound();

                // Check duplicate email
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Id != student.UserId);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already used by another user.");
                    ViewBag.TeacherName = student.Teacher?.User?.FullName ?? "Not Assigned";

                    return View(model);
                }

                // Check duplicate Employee ID
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(t => t.AdmissionNumber == model.AdmissionNumber && t.Id != model.Id);

                if (existingStudent != null)
                {
                    ModelState.AddModelError("AdmissionNumber", "This Addmission Number is already used by another user.");
                    return View(model);
                }

                // Update User
                student.User.FullName = model.FullName;
                student.User.Email = model.Email;
                student.User.UserName = model.Email;

                // Update Student
                student.AdmissionNumber = model.AdmissionNumber;
                student.Section = model.Section;
                student.Class = model.Class;
                student.TeacherId = model.TeacherId;
                student.IsActive = model.IsActive;

                await _userManager.UpdateAsync(student.User);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Student '{model.FullName}' updated successfully!";
                return RedirectToAction("ViewStudents");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Database error: {ex.Message}");

                // Get teacher name again for the view
                var student = await _context.Students
                    .Include(s => s.Teacher)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (student != null)
                {
                    ViewBag.TeacherName = student.Teacher?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");

                // Get teacher name again for the view
                var student = await _context.Students
                    .Include(s => s.Teacher)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (student != null)
                {
                    ViewBag.TeacherName = student.Teacher?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }
        }
        // ===== SOFT DELETE STUDENT =====
        [HttpGet]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("ViewStudents");
            }
            var adminUser = await _userManager.GetUserAsync(User);
            string studentName = student.User.FullName;


            // Soft delete - just deactivate
            student.IsActive = false;
            student.User.IsActive = false;
            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogger.LogAsync(
               "Delete Teacher",
               adminUser.FullName,
                $"Student '{studentName}' was deactivated by {adminUser.FullName}"
           );
           
            TempData["Success"] = $"Student '{student.User.FullName}' has been deactivated successfully.";
            return RedirectToAction("ViewStudents");
        }
        // ===== REACTIVATE STUDENT =====
        [HttpGet]
        public async Task<IActionResult> ReactivateStudent(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("ViewStudents");
            }

            // Reactivate
            student.IsActive = true;
            student.User.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Student '{student.User.FullName}' has been reactivated successfully.";
            return RedirectToAction("ViewStudents");
        }
















        //=======PARENT MANAGEMENT======//





        [HttpGet]
        public async Task<IActionResult> AddParent()
        {

            var model = new AddParentViewModel
            {
             
                // Generate Password
                Password = GenerateRandomPassword()
            };

            ViewBag.Students = new SelectList(
                await _context.Students
                .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                    })
                    .ToListAsync(),
                "Id", "Name"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddParent(AddParentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Password = GenerateRandomPassword();

                ViewBag.Students = new SelectList(
                    await _context.Students
                        .Include(s => s.User)
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            Id = s.Id,
                            Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                model.Password = GenerateRandomPassword();

                ViewBag.Students = new SelectList(
                    await _context.Students
                        .Include(s => s.User)
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            Id = s.Id,
                            Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }

            // Check if phone number already exists
            if (await _context.Parents.AnyAsync(p => p.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number already exists.");
                model.Password = GenerateRandomPassword();

                ViewBag.Students = new SelectList(
                    await _context.Students
                        .Include(s => s.User)
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            Id = s.Id,
                            Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }

            //Check if student already has this relationship
            var existingRelation = await _context.StudentParents
                .AnyAsync(sp => sp.StudentId == model.StudentId &&
                                sp.Relationship ==model.Relationship &&
                                sp.IsActive);

            if (existingRelation)
            {
                ModelState.AddModelError("Relationship", $"Student already has a {model.Relationship} linked.");
                model.Password = GenerateRandomPassword();

                ViewBag.Students = new SelectList(
                    await _context.Students
                        .Include(s => s.User)
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            Id = s.Id,
                            Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // AUTO-GENERATE PASSWORD
                var password = GenerateRandomPassword();
                // Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = "Parent",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true //Auto Confirm Email
                };
                var createResult = await _userManager.CreateAsync(user, model.Password);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    model.Password = GenerateRandomPassword();

                    return View(model);
                }

                // Add to Parent role
                await _userManager.AddToRoleAsync(user, "Parent");

                // Create Parent Profile
                var parent = new Parent
                {
                    UserId = user.Id,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Occupation = model.Occupation,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.Parents.AddAsync(parent);
                await _context.SaveChangesAsync();

                //Link Parent To Student

                var studentParent = new StudentParent
                {
                    StudentId = model.StudentId,
                    ParentId = parent.Id,
                    Relationship = model.Relationship,
                    IsPrimaryContact = true,
                    IsActive = true
                };

                await _context.StudentParents.AddAsync(studentParent);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = $"Parent {model.FullName} added successfully!";
                return RedirectToAction("AddParent");

            }
            catch (Exception ex)
            {

                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Failed to add parent: {ex.Message}");
                model.Password = GenerateRandomPassword();

                ViewBag.Students = new SelectList(
                    await _context.Students
                        .Include(s => s.User)
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            Id = s.Id,
                            Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }


        }
        [HttpGet]
        public  async Task<IActionResult> ViewParents()
        {
            var parents = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.StudentParents)
                    .ThenInclude(sp => sp.Student)
                        .ThenInclude(s => s.User)
               
                .ToListAsync();

            return View(parents);
        }

        //EDIT: PARENT
        
        // ===== EDIT PARENT - GET =====
        [HttpGet]
        public async Task<IActionResult> EditParent(int? id)
        {
            if (id == null)
                return NotFound();

            var parent = await _context.Parents
                .Include(t => t.User)
                .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (parent == null)
                return NotFound();

            // Get the first student linked to this parent
            var firstStudentParent = parent.StudentParents?.FirstOrDefault();
            int? studentId = firstStudentParent?.StudentId;
            string studentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";

            var model = new EditParentViewModel
            {
                Id = parent.Id,
                FullName = parent.User.FullName,
                Email = parent.User.Email,
                PhoneNumber = parent.PhoneNumber,
                Address = parent.Address,
                Occupation = parent.Occupation,
                StudentId = studentId,  // Get from StudentParents, not from User.Student
                StudentName = studentName,  // Add this to ViewModel
                IsActive = parent.IsActive
            };

            // Optional: Set ViewBag for student name if not using ViewModel property
            ViewBag.StudentName = studentName;

            return View(model);
        }

        // ===== EDIT PARENT - POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParent(EditParentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, get parent data again for the view
                var parent = await _context.Parents
                    .Include(t => t.User)
                    .Include(s => s.StudentParents)
                        .ThenInclude(sp => sp.Student)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (parent != null)
                {
                    var firstStudentParent = parent.StudentParents?.FirstOrDefault();
                    ViewBag.StudentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }

            try
            {
                var parent = await _context.Parents
                    .Include(t => t.User)
                    .Include(s => s.StudentParents)
                        .ThenInclude(sp => sp.Student)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (parent == null)
                    return NotFound();

                // Check duplicate email
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Id != parent.UserId);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already used by another user.");

                    var firstStudentParent = parent.StudentParents?.FirstOrDefault();
                    ViewBag.StudentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";
                    return View(model);
                }

                // Check duplicate Phone Number
                var existingParent = await _context.Parents
                    .FirstOrDefaultAsync(t => t.PhoneNumber == model.PhoneNumber && t.Id != model.Id);

                if (existingParent != null)
                {
                    ModelState.AddModelError("PhoneNumber", "This Phone Number is already used by another user.");

                    var firstStudentParent = parent.StudentParents?.FirstOrDefault();
                    ViewBag.StudentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";
                    return View(model);
                }

                // Update User
                parent.User.FullName = model.FullName;
                parent.User.Email = model.Email;
                parent.User.UserName = model.Email;

                // Update Parent
                parent.PhoneNumber = model.PhoneNumber;
                parent.Address = model.Address;
                parent.Occupation = model.Occupation;
                parent.IsActive = model.IsActive;

                // If StudentId changed, update the relationship
                if (model.StudentId.HasValue)
                {
                    // Remove existing StudentParent relationships
                    var existingRelationships = parent.StudentParents.ToList();
                    foreach (var rel in existingRelationships)
                    {
                        _context.StudentParents.Remove(rel);
                    }

                    // Add new relationship
                    var newRelationship = new StudentParent
                    {
                        StudentId = model.StudentId.Value,
                        ParentId = parent.Id,
                        Relationship = model.Relationship ?? "Parent",
                        IsPrimaryContact = true,
                        IsActive = true
                    };
                    await _context.StudentParents.AddAsync(newRelationship);
                }

                await _userManager.UpdateAsync(parent.User);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Parent '{model.FullName}' updated successfully!";
                return RedirectToAction("ViewParents");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Database error: {ex.Message}");

                var parent = await _context.Parents
                    .Include(t => t.User)
                    .Include(s => s.StudentParents)
                        .ThenInclude(sp => sp.Student)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (parent != null)
                {
                    var firstStudentParent = parent.StudentParents?.FirstOrDefault();
                    ViewBag.StudentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");

                var parent = await _context.Parents
                    .Include(t => t.User)
                    .Include(s => s.StudentParents)
                        .ThenInclude(sp => sp.Student)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (parent != null)
                {
                    var firstStudentParent = parent.StudentParents?.FirstOrDefault();
                    ViewBag.StudentName = firstStudentParent?.Student?.User?.FullName ?? "Not Assigned";
                }
                return View(model);
            }
        }
        // ===== SOFT DELETE PARENT =====
        [HttpGet]
        public async Task<IActionResult> DeleteParent(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var parent = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.StudentParents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (parent == null)
            {
                TempData["Error"] = "Parent not found.";
                return RedirectToAction("ViewParents");
            }
            var adminUser = await _userManager.GetUserAsync(User);
            string parentName = parent.User.FullName;

            // Soft delete - deactivate parent
            parent.IsActive = false;
            parent.User.IsActive = false;

            // Also deactivate all StudentParent relationships
            if (parent.StudentParents != null && parent.StudentParents.Any())
            {
                foreach (var sp in parent.StudentParents)
                {
                    sp.IsActive = false;
                }
            }

            // Log the action
            await _auditLogger.LogAsync(
               "Delete Teacher",
               adminUser.FullName,
                $"Student '{parentName}' was deactivated by {adminUser.FullName}"
           ); 
            


            await _context.SaveChangesAsync();

            TempData["Success"] = $"Parent '{parent.User.FullName}' has been deactivated successfully.";
            return RedirectToAction("ViewParents");
        }

        // ===== REACTIVATE PARENT =====
[HttpGet]
public async Task<IActionResult> ReactivateParent(int id)
{
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

    var parent = await _context.Parents
        .Include(p => p.User)
        .Include(p => p.StudentParents)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (parent == null)
    {
        TempData["Error"] = "Parent not found.";
        return RedirectToAction("ViewParents");
    }
            var adminUser = await _userManager.GetUserAsync(User);
            string parentName = parent.User.FullName;
            // Log the action
            
            // Reactivate parent
            parent.IsActive = true;
    parent.User.IsActive = true;

    // Also reactivate all StudentParent relationships
    if (parent.StudentParents != null && parent.StudentParents.Any())
    {
        foreach (var sp in parent.StudentParents)
        {
            sp.IsActive = true;
        }
    }
            // Log the action
            await _auditLogger.LogAsync(
               "Delete Teacher",
               adminUser.FullName,
                $"Student '{parentName}' was deactivated by {adminUser.FullName}");
    await _context.SaveChangesAsync();

    TempData["Success"] = $"Parent '{parent.User.FullName}' has been reactivated successfully.";
    return RedirectToAction("ViewParents");
  }








        public IActionResult SchoolInfo()
        {
          
            var schoolInfoList = _context.SchoolInfos.ToList();
            return View(schoolInfoList);
        }

        // GET: SchoolInfo/Create
        public IActionResult AddSchool()
        {
           

            return View();
        }

        // POST: SchoolInfo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchool([Bind("SchoollName,Address,PhoneNumber,Email,WebsiteUrl")] SchoolInfo schoolInfo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schoolInfo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "School information added successfully!";
                return RedirectToAction(nameof(SchoolInfo));
            }

            TempData["ErrorMessage"] = "Failed to add school information. Please check the input and try again.";
            return View(schoolInfo);
        }

        public IActionResult EditSchool()
        {
           

            var schoolInfo = _context.SchoolInfos.FirstOrDefault();
            if (schoolInfo == null)
            {
                return NotFound();
            }
            return View(schoolInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchool([Bind("SchoolInfoId,SchoolName,Address,PhoneNumber,Email,WebsiteUrl")] SchoolInfo schoolInfo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schoolInfo);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "School information updated successfully!";
                    return RedirectToAction(nameof(SchoolInfo));
                }
                catch (DbUpdateConcurrencyException)
                {
                   
                    
                        TempData["ErrorMessage"] = "Error updating  school information. Please try again.";
                        throw;
                    
                }
            }

            TempData["ErrorMessage"] = "Failed to update hospital information. Please check the input and try again.";
            return View(schoolInfo);
        }











        // ==================== COURSE MANAGEMENT ====================

        // GET: Courses List

        // GET: View Courses (Show both active and inactive)
        [HttpGet]
        public async Task<IActionResult> ViewCourses(string searchTerm, int page = 1, int pageSize = 10, bool showInactive = false)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var query = _context.Courses
                .Include(c => c.Classes)
                .AsQueryable();

            // Filter by active status
            if (!showInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.CourseName.Contains(searchTerm) ||
                    c.CourseCode.Contains(searchTerm) ||
                    (c.CourseDescription != null && c.CourseDescription.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var courses = await query
                .OrderBy(c => c.CourseName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PageSize = pageSize;
            ViewBag.ShowInactive = showInactive;

            return View(courses);
        }

        // GET: Create Course
        [HttpGet]
        public IActionResult CreateCourse()
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Create Course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            // Check if course code already exists (including inactive)
            if (await _context.Courses.AnyAsync(c => c.CourseCode == model.CourseCode))
            {
                ModelState.AddModelError("CourseCode", "This course code already exists.");
                return View(model);
            }

            try
            {
                var course = new Course
                {
                    CourseName = model.CourseName,
                    CourseCode = model.CourseCode,
                    CourseDescription = model.CourseDescription,
                    IsActive = true
                };

                await _context.Courses.AddAsync(course);
                await _context.SaveChangesAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Create Course",
                    adminUser.FullName,
                    $"Course '{model.CourseName}' ({model.CourseCode}) was created by {adminUser.FullName}"
                );

                TempData["Success"] = $"Course '{model.CourseName}' created successfully!";
                return RedirectToAction(nameof(ViewCourses));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating course: {ex.Message}");
                return View(model);
            }
        }

        // GET: Edit Course
        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(ViewCourses));
            }

            var model = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                CourseDescription = course.CourseDescription,
                IsActive = course.IsActive
            };

            return View(model);
        }

        // POST: Edit Course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(EditCourseViewModel model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var course = await _context.Courses.FindAsync(model.CourseId);

            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(ViewCourses));
            }

            // Check if course code is taken by another course (including inactive)
            if (await _context.Courses.AnyAsync(c => c.CourseCode == model.CourseCode && c.CourseId != model.CourseId))
            {
                ModelState.AddModelError("CourseCode", "This course code is already used by another course.");
                return View(model);
            }

            try
            {
                string oldName = course.CourseName;
                course.CourseName = model.CourseName;
                course.CourseCode = model.CourseCode;
                course.CourseDescription = model.CourseDescription;
                course.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Edit Course",
                    adminUser.FullName,
                    $"Course '{oldName}' was updated to '{model.CourseName}' by {adminUser.FullName}"
                );

                TempData["Success"] = $"Course '{model.CourseName}' updated successfully!";
                return RedirectToAction(nameof(ViewCourses));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating course: {ex.Message}");
                return View(model);
            }
        }

        // ===== SOFT DELETE COURSE =====
        [HttpGet]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var course = await _context.Courses
                .Include(c => c.Classes)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(ViewCourses));
            }

            // Check if course has active classes
            if (course.Classes != null && course.Classes.Any(c => c.IsActive))
            {
                var activeClassCount = course.Classes.Count(c => c.IsActive);
                TempData["Error"] = $"Cannot delete course '{course.CourseName}' because it has {activeClassCount} active class(es) associated with it. Please deactivate or delete the classes first.";
                return RedirectToAction(nameof(ViewCourses));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            string courseName = course.CourseName;

            // SOFT DELETE - Just deactivate
            course.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Delete Course",
                adminUser.FullName,
                $"Course '{courseName}' was deactivated by {adminUser.FullName}"
            );

            TempData["Success"] = $"Course '{courseName}' has been deactivated successfully.";
            return RedirectToAction(nameof(ViewCourses));
        }

        // ===== REACTIVATE COURSE =====
        [HttpGet]
        public async Task<IActionResult> ReactivateCourse(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                TempData["Error"] = "Course not found.";
                return RedirectToAction(nameof(ViewCourses));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            string courseName = course.CourseName;

            // Reactivate
            course.IsActive = true;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Reactivate Course",
                adminUser.FullName,
                $"Course '{courseName}' was reactivated by {adminUser.FullName}"
            );

            TempData["Success"] = $"Course '{courseName}' has been reactivated successfully!";
            return RedirectToAction(nameof(ViewCourses));
        }



















        // ==================== CLASS MANAGEMENT ====================
        // GET: View Classes (Show both active and inactive)
        [HttpGet]
        public async Task<IActionResult> ViewClasses(string searchTerm, int page = 1, int pageSize = 10, bool showInactive = false)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var query = _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Enrollments)
                .AsQueryable();

            // If showInactive is false, only show active classes
            if (!showInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.ClassName.Contains(searchTerm) ||
                    c.Course.CourseName.Contains(searchTerm) ||
                    c.Course.CourseCode.Contains(searchTerm) ||
                    c.Teacher.User.FullName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var classes = await query
                .OrderBy(c => c.ClassName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalPages = totalPages;
            ViewBag.ShowInactive = showInactive;

            return View(classes);
        }

        // GET: Create Class
        [HttpGet]
        public async Task<IActionResult> CreateClass()
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
            ViewBag.Teachers = new SelectList(
                await _context.Teachers
                    .Include(t => t.User)
                    .Where(t => t.IsActive)
                    .Select(t => new
                    {
                        Id = t.Id,
                        Name = t.User.FullName + " (" + t.EmployeeId + ")"
                    })
                    .ToListAsync(),
                "Id", "Name"
            );
            return View();
        }

        // POST: Create Class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CreateClassViewModel model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                ViewBag.Teachers = new SelectList(
                    await _context.Teachers
                        .Include(t => t.User)
                        .Where(t => t.IsActive)
                        .Select(t => new
                        {
                            Id = t.Id,
                            Name = t.User.FullName + " (" + t.EmployeeId + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }

            try
            {
                // Check if class name already exists (including inactive)
                if (await _context.Classes.AnyAsync(c => c.ClassName == model.ClassName))
                {
                    ModelState.AddModelError("ClassName", "This class name already exists.");
                    ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                    ViewBag.Teachers = new SelectList(
                        await _context.Teachers
                            .Include(t => t.User)
                            .Where(t => t.IsActive)
                            .Select(t => new
                            {
                                Id = t.Id,
                                Name = t.User.FullName + " (" + t.EmployeeId + ")"
                            })
                            .ToListAsync(),
                        "Id", "Name"
                    );
                    return View(model);
                }

                var classEntity = new Class
                {
                    ClassName = model.ClassName,
                    CourseId = model.CourseId,
                    TeacherId = model.TeacherId,
                    Capacity = model.Capacity,
                    IsActive = true
                };

                await _context.Classes.AddAsync(classEntity);
                await _context.SaveChangesAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                var course = await _context.Courses.FindAsync(model.CourseId);
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == model.TeacherId);

                await _auditLogger.LogAsync(
                    "Create Class",
                    adminUser.FullName,
                    $"Class '{model.ClassName}' was created for Course '{course?.CourseName}' with Teacher '{teacher?.User?.FullName}'"
                );

                TempData["Success"] = $"Class '{model.ClassName}' created successfully!";
                return RedirectToAction(nameof(ViewClasses));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating class: {ex.Message}");
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                ViewBag.Teachers = new SelectList(
                    await _context.Teachers
                        .Include(t => t.User)
                        .Where(t => t.IsActive)
                        .Select(t => new
                        {
                            Id = t.Id,
                            Name = t.User.FullName + " (" + t.EmployeeId + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }
        }

        // GET: Edit Class
        [HttpGet]
        public async Task<IActionResult> EditClass(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var classEntity = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found.";
                return RedirectToAction(nameof(ViewClasses));
            }

            var model = new EditClassViewModel
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName,
                CourseId = classEntity.CourseId,
                TeacherId = classEntity.TeacherId ?? 0,
                Capacity = classEntity.Capacity,
                IsActive = classEntity.IsActive
            };

            ViewBag.Courses = new SelectList(
                await _context.Courses.ToListAsync(),
                "CourseId", "CourseName",
                classEntity.CourseId
            );

            ViewBag.Teachers = new SelectList(
                await _context.Teachers
                    .Include(t => t.User)
                    .Where(t => t.IsActive)
                    .Select(t => new
                    {
                        Id = t.Id,
                        Name = t.User.FullName + " (" + t.EmployeeId + ")"
                    })
                    .ToListAsync(),
                "Id", "Name",
                classEntity.TeacherId
            );

            return View(model);
        }

        // POST: Edit Class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(EditClassViewModel model)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                ViewBag.Teachers = new SelectList(
                    await _context.Teachers
                        .Include(t => t.User)
                        .Where(t => t.IsActive)
                        .Select(t => new
                        {
                            Id = t.Id,
                            Name = t.User.FullName + " (" + t.EmployeeId + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }

            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == model.ClassId);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found.";
                return RedirectToAction(nameof(ViewClasses));
            }

            try
            {
                // Check if class name is taken by another class (including inactive)
                if (await _context.Classes.AnyAsync(c => c.ClassName == model.ClassName && c.ClassId != model.ClassId))
                {
                    ModelState.AddModelError("ClassName", "This class name already exists.");
                    ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                    ViewBag.Teachers = new SelectList(
                        await _context.Teachers
                            .Include(t => t.User)
                            .Where(t => t.IsActive)
                            .Select(t => new
                            {
                                Id = t.Id,
                                Name = t.User.FullName + " (" + t.EmployeeId + ")"
                            })
                            .ToListAsync(),
                        "Id", "Name"
                    );
                    return View(model);
                }

                // Check if new capacity is less than current enrollment
                if (model.Capacity < classEntity.Enrollments.Count)
                {
                    ModelState.AddModelError("Capacity", $"Cannot reduce capacity below current enrollment ({classEntity.Enrollments.Count} students).");
                    ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                    ViewBag.Teachers = new SelectList(
                        await _context.Teachers
                            .Include(t => t.User)
                            .Where(t => t.IsActive)
                            .Select(t => new
                            {
                                Id = t.Id,
                                Name = t.User.FullName + " (" + t.EmployeeId + ")"
                            })
                            .ToListAsync(),
                        "Id", "Name"
                    );
                    return View(model);
                }

                string oldName = classEntity.ClassName;
                classEntity.ClassName = model.ClassName;
                classEntity.CourseId = model.CourseId;
                classEntity.TeacherId = model.TeacherId;
                classEntity.Capacity = model.Capacity;
                classEntity.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Edit Class",
                    adminUser.FullName,
                    $"Class '{oldName}' was updated to '{model.ClassName}' by {adminUser.FullName}"
                );

                TempData["Success"] = $"Class '{model.ClassName}' updated successfully!";
                return RedirectToAction(nameof(ViewClasses));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating class: {ex.Message}");
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "CourseName");
                ViewBag.Teachers = new SelectList(
                    await _context.Teachers
                        .Include(t => t.User)
                        .Where(t => t.IsActive)
                        .Select(t => new
                        {
                            Id = t.Id,
                            Name = t.User.FullName + " (" + t.EmployeeId + ")"
                        })
                        .ToListAsync(),
                    "Id", "Name"
                );
                return View(model);
            }
        }

        // ===== SOFT DELETE CLASS =====
        [HttpGet]
        public async Task<IActionResult> DeleteClass(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found.";
                return RedirectToAction(nameof(ViewClasses));
            }

            // Check if class has active enrollments
            if (classEntity.Enrollments != null && classEntity.Enrollments.Any(e => e.IsActive))
            {
                var activeEnrollments = classEntity.Enrollments.Count(e => e.IsActive);
                TempData["Error"] = $"Cannot delete class '{classEntity.ClassName}' because it has {activeEnrollments} active student(s) enrolled. Please remove all enrollments first.";
                return RedirectToAction(nameof(ViewClasses));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            string className = classEntity.ClassName;

            // SOFT DELETE - Just deactivate
            classEntity.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Delete Class",
                adminUser.FullName,
                $"Class '{className}' was deactivated by {adminUser.FullName}"
            );

            TempData["Success"] = $"Class '{className}' has been deactivated successfully.";
            return RedirectToAction(nameof(ViewClasses));
        }

        // ===== REACTIVATE CLASS =====
        [HttpGet]
        public async Task<IActionResult> ReactivateClass(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var classEntity = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found.";
                return RedirectToAction(nameof(ViewClasses));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            string className = classEntity.ClassName;

            // Reactivate
            classEntity.IsActive = true;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Reactivate Class",
                adminUser.FullName,
                $"Class '{className}' was reactivated by {adminUser.FullName}"
            );

            TempData["Success"] = $"Class '{className}' has been reactivated successfully!";
            return RedirectToAction(nameof(ViewClasses));
        }


















        // ==================== ENROLLMENT MANAGEMENT ====================

        // GET: Manage Enrollments
        [HttpGet]
        public async Task<IActionResult> ManageEnrollments(int? classId)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            // Get all active classes for dropdown
            var classes = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .Select(c => new
                {
                    Id = c.ClassId,
                    Name = c.ClassName + " (" + c.Course.CourseName + ") - " + c.Enrollments.Count + "/" + c.Capacity
                })
                .ToListAsync();

            ViewBag.Classes = new SelectList(classes, "Id", "Name", classId);

            if (classId.HasValue)
            {
                // Get the selected class with details
                var selectedClass = await _context.Classes
                    .Include(c => c.Course)
                    .Include(c => c.Teacher)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Student)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(c => c.ClassId == classId.Value && c.IsActive);

                if (selectedClass == null)
                {
                    TempData["Error"] = "Class not found.";
                    return View(new List<Enrollment>());
                }

                ViewBag.SelectedClass = selectedClass;
                ViewBag.EnrollmentCount = selectedClass.Enrollments.Count;
                ViewBag.AvailableSpots = selectedClass.Capacity - selectedClass.Enrollments.Count;

                // Get all active students not enrolled in this class
                var enrolledStudentIds = selectedClass.Enrollments.Select(e => e.StudentId).ToList();
                var availableStudents = await _context.Students
                    .Include(s => s.User)
                    .Where(s => s.IsActive && !enrolledStudentIds.Contains(s.Id))
                    .OrderBy(s => s.User.FullName)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Name = s.User.FullName + " (" + s.AdmissionNumber + ")"
                    })
                    .ToListAsync();

                ViewBag.AvailableStudents = new SelectList(availableStudents, "Id", "Name");

                return View(selectedClass.Enrollments.ToList());
            }

            return View(new List<Enrollment>());
        }

        // POST: Enroll Student
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(int studentId, int classId)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            try
            {
                // Check if class exists and is active
                var classEntity = await _context.Classes
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.ClassId == classId && c.IsActive);

                if (classEntity == null)
                {
                    TempData["Error"] = "Class not found or is inactive.";
                    return RedirectToAction(nameof(ManageEnrollments), new { classId });
                }

                // Check if student exists and is active
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive);

                if (student == null)
                {
                    TempData["Error"] = "Student not found or is inactive.";
                    return RedirectToAction(nameof(ManageEnrollments), new { classId });
                }

                // Check if class is at capacity
                if (classEntity.Enrollments.Count >= classEntity.Capacity)
                {
                    TempData["Error"] = $"Class '{classEntity.ClassName}' has reached its capacity of {classEntity.Capacity} students.";
                    return RedirectToAction(nameof(ManageEnrollments), new { classId });
                }

                // Check for duplicate enrollment
                if (await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.ClassId == classId))
                {
                    TempData["Error"] = $"Student '{student.User.FullName}' is already enrolled in this class.";
                    return RedirectToAction(nameof(ManageEnrollments), new { classId });
                }

                // Create enrollment
                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    ClassId = classId,
                    EnrollmentDate = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.Enrollments.AddAsync(enrollment);
                await _context.SaveChangesAsync();

                var adminUser = await _userManager.GetUserAsync(User);
                await _auditLogger.LogAsync(
                    "Enroll Student",
                    adminUser.FullName,
                    $"Student '{student.User.FullName}' was enrolled in class '{classEntity.ClassName}' by {adminUser.FullName}"
                );

                TempData["Success"] = $"Student '{student.User.FullName}' enrolled successfully in '{classEntity.ClassName}'!";
                return RedirectToAction(nameof(ManageEnrollments), new { classId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error enrolling student: {ex.Message}";
                return RedirectToAction(nameof(ManageEnrollments), new { classId });
            }
        }




        // GET: Remove Enrollment
        [HttpGet]
        public async Task<IActionResult> RemoveEnrollment(int enrollmentId)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
            {
                TempData["Error"] = "Enrollment not found.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            string studentName = enrollment.Student.User.FullName;
            string className = enrollment.Class.ClassName;
            int classId = enrollment.ClassId;

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Remove Enrollment",
                adminUser.FullName,
                $"Student '{studentName}' was removed from class '{className}' by {adminUser.FullName}"
            );

            TempData["Success"] = $"Student '{studentName}' has been removed from class '{className}'.";
            return RedirectToAction(nameof(ManageEnrollments), new { classId });
        }

        // GET: View Class Roster
        [HttpGet]
        public async Task<IActionResult> ClassRoster(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Login", "Account");

            var classEntity = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (classEntity == null)
            {
                TempData["Error"] = "Class not found.";
                return RedirectToAction(nameof(ViewClasses));
            }

            ViewBag.EnrollmentCount = classEntity.Enrollments.Count;
            ViewBag.AvailableSpots = classEntity.Capacity - classEntity.Enrollments.Count;

            return View(classEntity);
        }


    }
}
