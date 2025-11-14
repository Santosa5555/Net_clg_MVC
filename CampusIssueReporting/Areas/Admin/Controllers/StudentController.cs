using CampusIssueReporting.Models;
using CampusIssueReporting.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusIssueReporting.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Admin/Student
        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 50)
        {
            // Query users in Student role (or users that are not Admins)
            var usersQuery = _userManager.Users.AsQueryable();

            // Filter: prefer to show users who are in "Student" role. If you have a RoleUser join, you can join; otherwise filter by not-admin
            // We'll filter as: exclude Admins (safer) and optionally search by name/email/rollno
            var adminIds = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where r.Name == "Admin"
                                  select ur.UserId).ToListAsync();

            usersQuery = usersQuery.Where(u => !adminIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(s)) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(s))
                );
                ViewData["Search"] = search;
            }

            var total = await usersQuery.CountAsync();
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);

            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Page"] = page;
            ViewData["PageCount"] = pageCount;
            ViewData["PageSize"] = pageSize;
            ViewData["Total"] = total;

            return View(users);
        }

        // GET: Admin/Student/Details/id
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.Users
                .Include(u => u.Issues) // optional; ensures EF loads related issues if navigation exists
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // Optionally get roles
            var roles = await _userManager.GetRolesAsync(user);
            ViewData["Roles"] = roles;

            return View(user);
        }

        // GET: Admin/Student/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Map only editable fields to a view-model (we'll use a lightweight DTO inline)
            var model = new StudentViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = (user as ApplicationUser)?.FullName // if you have FullName property
            };

            return View(model);
        }

        // POST: Admin/Student/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, StudentViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent changing admin flags here; this is only for student basic info
            user.UserName = model.UserName?.Trim();
            user.Email = model.Email?.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            // If your ApplicationUser has a FullName property:
            if (user is ApplicationUser appUser && model.FullName != null)
            {
                appUser.FullName = model.FullName.Trim();
            }

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // We will update email directly — if you enforce email confirmation flows, adapt accordingly.

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            TempData["SuccessMessage"] = "Student information updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Student/Delete/id
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // don't allow admin to delete themselves
            var currentAdminId = _userManager.GetUserId(User);
            var isSelf = currentAdminId == id;
            ViewData["IsSelf"] = isSelf;

            // check if the user has Admin role
            var roles = await _userManager.GetRolesAsync(user);
            ViewData["IsAdminAccount"] = roles.Contains("Admin");

            return View(user);
        }

        // POST: Admin/Student/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting an admin account
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["ErrorMessage"] = "Cannot delete an Admin account.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Prevent admin deleting themselves
            var currentAdminId = _userManager.GetUserId(User);
            if (currentAdminId == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // OPTIONAL: If you want to prevent deleting users who have open issues, check issues table.
            var hasOpenIssues = await _context.Issues.AnyAsync(i => i.ReporterId == id && i.Status != IssueStatus.Resolved /* or your status logic */);
            if (hasOpenIssues)
            {
                TempData["ErrorMessage"] = "Student has open issues. Reassign or close them before deleting the account.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Remove user via UserManager (removes identity entries)
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Unable to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["SuccessMessage"] = "Student account deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

}
