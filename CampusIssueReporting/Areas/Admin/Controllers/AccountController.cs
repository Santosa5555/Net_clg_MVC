using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using CampusIssueReporting.Models;
using CampusIssueReporting.ViewModel;
using Microsoft.AspNetCore.Authorization;
namespace CampusIssueReporting.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult AdminLogin(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                // Log ModelState errors
                var errors = ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .Select(kvp => new
                    {
                        Field = kvp.Key,
                        Errors = kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    }).ToList();

                _logger.LogWarning("AdminLogin: ModelState invalid. Errors: {@Errors}", errors);

                // Log exact form payload the server received (for diagnosing mismatched names)
                var form = Request.HasFormContentType ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()) : null;
                _logger.LogWarning("AdminLogin: Request.Form: {@Form}", form);

                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("AdminLogin: user not found for email {Email}", model.Email);
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // Ensure the user is in Admin role
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                _logger.LogWarning("AdminLogin: user {UserId} ({Email}) is not in Admin role.", user.Id, model.Email);
                ModelState.AddModelError("", "User is not an admin.");
                return View(model);
            }

            // PasswordSignInAsync expects username (usually UserName)
            var result = await _signInManager.PasswordSignInAsync(user.UserName ?? model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("AdminLogin: {User} ({Email}) logged in successfully.", user.UserName, model.Email);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("AdminDashboard", "Account", new { area = "Admin" });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("AdminLogin: user {User} locked out.", user.UserName);
                ModelState.AddModelError("", "Account locked out.");
                return View(model);
            }

            _logger.LogWarning("AdminLogin: invalid credentials for {User}. Result: {@Result}", user.UserName, new { result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor });
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("AdminLogin");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }

}
