using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Helpers;
using School_Management_System.Models;
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

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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
        private async Task<string> GenerateAdmissionNumberAsync()
        {
            var year = DateTime.UtcNow.Year.ToString().Substring(2);
            var count = await _context.Students.CountAsync() + 1;
            return $"STU-{year}-{count:D4}";
        }
        [HttpGet]
        public IActionResult GenerateCredentials()
        {
            return Json(new
            {
                password = GenerateRandomPassword(),
                employeeId = GenerateEmployeeIdSync()
            });
        }


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
                .Where(t => t.IsActive)
                .ToListAsync();

            return View(teachers);
        }

        //Student management

        [HttpGet]
        public async  Task<IActionResult> AddStudent()
        {
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
                var admissionNumber = await GenerateAdmissionNumberAsync();

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
               .Where(s => s.IsActive)
               .ToListAsync();

            return View(students);
        }


        //Parent Management

        [HttpGet]
        public async Task<IActionResult> AddParent()
        {
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
                .Where(p => p.IsActive)
                .ToListAsync();

            return View(parents);
        }



      
    }
}
