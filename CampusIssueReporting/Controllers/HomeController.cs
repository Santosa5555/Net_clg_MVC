using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CampusIssueReporting.Models;
using CampusIssueReporting.Utilities;
using Microsoft.AspNetCore.Identity;
namespace CampusIssueReporting.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return await RedirectHelper.RedirectToRoleDashboardAsync(_userManager, User, this);
        }

        // For non-authenticated users, show the issue feed
        // Redirect to Issue/Index which now allows anonymous access
        return RedirectToAction("Index", "Issue");
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
