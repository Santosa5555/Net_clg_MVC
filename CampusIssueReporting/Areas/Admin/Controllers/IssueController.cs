using System.Threading.Tasks;
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
        public IssueController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
        {
            var query = _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt);

            var total = await query.CountAsync();
            var pageCount = (int)System.Math.Ceiling(total / (double)pageSize);

            var issues = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewData["Page"] = page;
            ViewData["PageCount"] = pageCount;
            return View(issues);
        }

        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.Category)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .Include(i => i.Files)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            return View(issue); // admin-only detailed view
        }
    }
}
