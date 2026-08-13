using GreenPipes.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using School_Management_System.Data;
using School_Management_System.Models;

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
                if (User.Identity.IsAuthenticated)
                {
                    // Redirect based on role
                    if (User.IsInRole("Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    else if (User.IsInRole("Teacher"))
                        return RedirectToAction("Dashboard", "Teacher");
                    else if (User.IsInRole("Student"))
                        return RedirectToAction("Profile", "Student");
                    else if (User.IsInRole("Parent"))
                        return RedirectToAction("Dashboard", "Parent");
                    else
                        return RedirectToAction("Login", "Account");
                }
               
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


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ChangePasswordViewModel
            {
                Email = user.Email // Pre-fill email field
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);


            //Get The current User

            var user = await _userManager.GetUserAsync(User);
            if(user== null)
            {
                return RedirectToAction("Login", "Account");
            }

            //Verifying if  Email matches
            if(!user.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Email address does not match your account");
                return View(model);

            }

            //Verifying Current Password


            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

            if (!isPasswordValid)
            {
                ModelState.AddModelError("", "Password does not match your Account");
                return View(model);
            }

            //Prevent from using the same Password

            if(model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "New PassWord must not be same as current password");
                return View(model);
            }

            //Change password

            var result =await _userManager.ChangePasswordAsync(user,model.CurrentPassword,model.NewPassword);

            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            //Refresing sign-in to maintain session
            await _signInManager.RefreshSignInAsync(user);

            //Log the password change
            await LogPasswordChange(user.Id);

            TempData["Success"] = "Your password has been changed successfully!";
            return RedirectToAction("ChangePasswordConfirmation");
        }

        private async Task LogPasswordChange(string userId)
        {
           // track changes
            var logEntry = new
            {
                UserId = userId,
                ChangedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

        }
        [HttpGet]
        [Authorize]
        public IActionResult ChangePasswordConfirmation()
        {
            return View();
        }

    }
}
