using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;

using School_Management_System.Models;
using School_Management_System.Services.Implementation;
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
        private readonly AuditLogger _auditLogger;

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
            ViewBag.TotalUsers = await _context.Users.CountAsync(u => u.IsActive);
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
       
        //// ===== VIEW AUDIT LOGS =====
        //[HttpGet]
        //public async Task<IActionResult> ViewLogs()
        //{
        //    if (!User.IsInRole("Admin"))
        //        return RedirectToAction("Login", "Account");

        //    try
        //    {
        //        var logs = await _context.AuditLogs
        //            .Include(l => l.User)
        //            .OrderByDescending(l => l.ActionDate)
        //            .Take(100)
        //            .ToListAsync();

        //        // Even if logs is empty, pass an empty list (not null)
        //        return View(logs ?? new List<AuditLog>());
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        TempData["Error"] = $"Error loading audit logs: {ex.Message}";
        //        return View(new List<AuditLog>());  // Pass empty list to avoid null reference
        //    }
        //}


    }
}
