using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIssueReporting.Models;

namespace CampusIssueReporting.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class IssueCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IssueCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/IssueCategory
        public async Task<IActionResult> Index()
        {
            var categories = await _context.IssueCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // GET: Admin/IssueCategory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var category = await _context.IssueCategories
                .Include(c => c.Issues) // optional: show related issues count
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // GET: Admin/IssueCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/IssueCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] IssueCategory category)
        {
            if (!ModelState.IsValid) return View(category);

            // Prevent duplicate names (case-insensitive)
            var exists = await _context.IssueCategories
                .AnyAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(IssueCategory.Name), "A category with that name already exists.");
                return View(category);
            }

            category.Name = category.Name.Trim();
            _context.IssueCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/IssueCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var category = await _context.IssueCategories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Admin/IssueCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] IssueCategory category)
        {
            if (id != category.Id) return BadRequest();

            if (!ModelState.IsValid) return View(category);

            var exists = await _context.IssueCategories
                .AnyAsync(c => c.Id != category.Id && c.Name.ToLower() == category.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(IssueCategory.Name), "Another category with that name already exists.");
                return View(category);
            }

            try
            {
                var dbCategory = await _context.IssueCategories.FindAsync(id);
                if (dbCategory == null) return NotFound();

                dbCategory.Name = category.Name.Trim();
                _context.Update(dbCategory);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.IssueCategories.AnyAsync(c => c.Id == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/IssueCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var category = await _context.IssueCategories
                .Include(c => c.Issues)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Admin/IssueCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.IssueCategories
                .Include(c => c.Issues)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // If you want to prevent deletion when issues exist:
            if (category.Issues != null && category.Issues.Any())
            {
                TempData["ErrorMessage"] = "Category cannot be deleted because it has associated issues. Reassign or delete those issues first.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.IssueCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
