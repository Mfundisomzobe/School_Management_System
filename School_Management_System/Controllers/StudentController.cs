using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

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
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Teacher)
                    .ThenInclude(t => t.User)
                .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Parent)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null)
                return RedirectToAction("AccessDenied", "Account");

            return View(student);
        }
    }
}
