using CampusIssueReporting.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampusIssueReporting.ViewModel;
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;

    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
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

            _logger.LogInformation("Login: ModelState invalid. Errors: {@Errors}", errors);

            // Log exact form payload the server received (for diagnosing mismatched names)
            var form = Request.HasFormContentType ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()) : null;
            _logger.LogInformation("Login: Request.Form: {@Form}", form);

            return View(model);
        }
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName ?? model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Issue");
        }

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
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

            _logger.LogWarning("Register: ModelState invalid. Errors: {@Errors}", errors);

            // Log exact form payload the server received
            var form = Request.HasFormContentType ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()) : null;
            _logger.LogWarning("Register: Request.Form: {@Form}", form);

            return View(model);
        }

        _logger.LogInformation("Register: Attempting to register user with email {Email}", model.Email);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Register: User {Email} created successfully. User ID: {UserId}", model.Email, user.Id);

            await _userManager.AddToRoleAsync(user, "Student");
            _logger.LogInformation("Register: User {Email} added to Student role.", model.Email);

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("Register: User {Email} signed in successfully.", model.Email);

            return RedirectToAction("Index", "Issue");
        }

        _logger.LogError("Register: Failed to create user {Email}. Errors: {@Errors}",
            model.Email, result.Errors.Select(e => new { e.Code, e.Description }));

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult StudentDashboard()
    {
        // load user-specific data via IssueService etc.
        return View();
    }
}



