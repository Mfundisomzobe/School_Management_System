using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.ViewModels;
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

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var parent = await _context.Parents
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (parent == null)
                return RedirectToAction("AccessDenied", "Account");

            var children = await _context.StudentParents
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.User)
                .Include(sp => sp.Student)
                    .ThenInclude(s => s.Teacher)
                        .ThenInclude(t => t.User)
                .Where(sp => sp.ParentId == parent.Id && sp.IsActive)
                .ToListAsync();

            ViewBag.ParentName = user.FullName;
            ViewBag.ChildCount = children.Count;

            return View(children);
        }

        public async Task<IActionResult> ViewChild(int studentId)
        {
            var user = await _userManager.GetUserAsync(User);

            var parent = await _context.Parents
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (parent == null)
                return RedirectToAction("AccessDenied", "Account");

            // Verify this parent is linked to the student
            var isLinked = await _context.StudentParents
                 .AnyAsync(sp => sp.ParentId == parent.Id &&
                                sp.StudentId == studentId &&
                                sp.IsActive);
            if (!isLinked)
                return RedirectToAction("Dashboard");


            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Teacher)
                    .ThenInclude(t => t.User)
                .Include(s => s.StudentParents)
                    .ThenInclude(sp => sp.Parent)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            // Get the relationship for this student
            var relationship = await _context.StudentParents
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId &&
                                          sp.ParentId == parent.Id);

            ViewBag.Relationship = relationship?.Relationship ?? "Unknown";

            return View(student);
        }
    }
}
