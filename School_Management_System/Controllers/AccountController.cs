using GreenPipes.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services.Interfaces;
using School_Management_System.ViewModels;

namespace School_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
           RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] =returnUrl;

            if(!ModelState.IsValid) 
            return View(model);

            //This doesn't count login failures towards account lockout
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                //Update last login time
                user.CreatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                //Redirect based on role
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                    return RedirectToAction("Dashboard", "Teacher");
                else if (await _userManager.IsInRoleAsync(user, "Student"))
                    return RedirectToAction("Profile", "Student");
                else if (await _userManager.IsInRoleAsync(user, "Parent"))
                    return RedirectToAction("Dashboard", "Parent");
                else
                    return RedirectToAction("Index", "Home");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account Locked out. Please ty again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public  IActionResult AccessDenied()
        {
           return View();
        }
    }
}
