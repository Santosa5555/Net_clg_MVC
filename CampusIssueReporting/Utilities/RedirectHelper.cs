using System.Security.Claims;
using CampusIssueReporting.Controllers;
using CampusIssueReporting.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace CampusIssueReporting.Utilities;

public static class RedirectHelper
{
    // Prioritize Admin over Student
    public static async Task<IActionResult> RedirectToRoleDashboardAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal user,
        Controller controller,
        string? returnUrl = null)
    {
        if (!user.Identity?.IsAuthenticated ?? false)
            return controller.RedirectToAction("Index", "Home");

        var appUser = await userManager.GetUserAsync(user);
        if (appUser == null)
            return controller.RedirectToAction("Index", "Home");

        if (!string.IsNullOrEmpty(returnUrl) && controller.Url.IsLocalUrl(returnUrl))
            return controller.Redirect(returnUrl);

        if (await userManager.IsInRoleAsync(appUser, "Admin"))
            return controller.RedirectToAction("AdminDashboard", "Account", new { area = "Admin" });

        if (await userManager.IsInRoleAsync(appUser, "Student"))
            return controller.RedirectToAction("StudentDashboard", "Account");

        return controller.RedirectToAction("Index", "Home");
    }
}
