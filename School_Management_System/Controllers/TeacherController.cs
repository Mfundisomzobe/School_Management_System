using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using System.Threading.Tasks;

namespace School_Management_System.Controllers
{
    [Authorize(Roles ="Teacher")]
    public class TeacherController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TeacherController(
             UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            
        }
        public async Task<IActionResult> Dasboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Parent)
                        .ThenInclude(p => p.User)
                .Where(s => s.TeacherId == teacher.Id && s.IsActive)
                .ToListAsync();

            ViewBag.TeacherName = user.FullName;
            ViewBag.StudentCount = students.Count;

            return View(students);
        }
    }
}
