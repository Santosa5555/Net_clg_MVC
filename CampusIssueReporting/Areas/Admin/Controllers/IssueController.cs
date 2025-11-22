using CampusIssueReporting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusIssueReporting.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class IssueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IssueController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Issue
        public async Task<IActionResult> Index(int page = 1, int pageSize = 50, int? categoryId = null, IssueStatus? status = null)
        {
            var query = _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.Category)
                .Include(i => i.Files)
                .Include(i => i.Comments)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(i => i.CategoryId == categoryId.Value);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            query = query.OrderByDescending(i => i.CreatedAt);

            var total = await query.CountAsync();
            var pageCount = (int)System.Math.Ceiling(total / (double)pageSize);

            var issues = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Page"] = page;
            ViewData["PageCount"] = pageCount;
            ViewData["Total"] = total;
            ViewData["PageSize"] = pageSize;

            // categories for filter dropdown
            ViewData["Categories"] = await _context.IssueCategories.OrderBy(c => c.Name).ToListAsync();
            ViewData["SelectedCategory"] = categoryId;
            ViewData["SelectedStatus"] = status;

            return View(issues);
        }

        // GET: Admin/Issue/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.AssignedAdmin)
                .Include(i => i.Category)
                .Include(i => i.Files)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();

            return View(issue);
        }

        // GET: Admin/Issue/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.AssignedAdmin)
                .Include(i => i.Category)
                .Include(i => i.Files)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();

            // Build list of admin users to assign
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUsers = new List<ApplicationUser>();
            if (adminRole != null)
            {
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                adminUsers = await _context.Users
                    .Where(u => adminUserIds.Contains(u.Id))
                    .OrderBy(u => u.UserName)
                    .ToListAsync();
            }

            ViewData["AdminUsers"] = adminUsers;
            ViewData["Priorities"] = Enum.GetValues(typeof(IssuePriority)).Cast<IssuePriority>().ToList();
            ViewData["Statuses"] = Enum.GetValues(typeof(IssueStatus)).Cast<IssueStatus>().ToList();

            return View(issue);
        }

        // POST: Admin/Issue/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? assignedAdminIdChoice, string? assignedAdminId, IssueStatus status, IssuePriority priority)
        {
            // Note: we accept assignedAdminId directly as string (user Id), but also show how to accept alternate inputs.
            var issue = await _context.Issues.FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null) return NotFound();

            // update fields
            issue.Status = status;
            issue.Priority = priority;

            // assignedAdminId passed from form (string userId or empty)
            if (!string.IsNullOrEmpty(assignedAdminId))
            {
                // verify user exists and is admin
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                var isAdminUser = false;
                if (adminRole != null)
                {
                    isAdminUser = await _context.UserRoles.AnyAsync(ur => ur.UserId == assignedAdminId && ur.RoleId == adminRole.Id);
                }

                if (isAdminUser)
                {
                    issue.AssignedAdminId = assignedAdminId;
                }
                else
                {
                    ModelState.AddModelError(nameof(issue.AssignedAdminId), "Selected user is not an admin.");
                }
            }
            else
            {
                issue.AssignedAdminId = null;
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Failed to update issue. See errors.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            issue.UpdatedAt = DateTime.UtcNow;
            _context.Issues.Update(issue);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Issue updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
