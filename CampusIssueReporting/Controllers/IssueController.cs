using CampusIssueReporting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusIssueReporting.Controllers
{
    [Authorize]
    public class IssueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IssueController> _logger;

        // configure allowed extensions and size
        private readonly string[] _permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx", ".txt" };
        private const long _maxFileSize = 10 * 1024 * 1024; // 10 MB per file

        public IssueController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ILogger<IssueController> logger)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Issue
        // Public homepage: show recent issues (students can view others)
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            _logger.LogInformation($"IssueController.Index called - Page: {page}, PageSize: {pageSize}");

            var query = _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.Category)
                .Include(i => i.Files)
                .Include(i => i.Comments)
                .OrderByDescending(i => i.CreatedAt)
                .AsQueryable();

            var total = await query.CountAsync();
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);

            var issues = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Page"] = page;
            ViewData["PageCount"] = pageCount;
            ViewData["Total"] = total;

            return View(issues);
        }

        // GET: /Issue/MyIssues
        public async Task<IActionResult> MyIssues()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            _logger.LogInformation($"IssueController.MyIssues called - UserId: {userId}");

            var issues = await _context.Issues
                .Where(i => i.ReporterId == userId)
                .Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        // GET: /Issue/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.IssueCategories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        // POST: /Issue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,CategoryId")] Issue issue, List<IFormFile> files)
        {
            _logger.LogInformation($"IssueController.Create POST called - Title: {issue.Title}, CategoryId: {issue.CategoryId}, FileCount: {files?.Count}");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            // Assign ReporterId and CreatedAt before validating ModelState
            issue.ReporterId = userId;
            issue.CreatedAt = DateTime.UtcNow;

            // Clear validation errors for fields we just assigned
            ModelState.Remove("ReporterId");
            ModelState.Remove("Reporter");

            if (!ModelState.IsValid)
            {

                ViewBag.Categories = await _context.IssueCategories.OrderBy(c => c.Name).ToListAsync();
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                    _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                return View(issue);
            }

            _logger.LogInformation($"IssueController.Create - Issue created by UserId: {userId}, IssueId will be assigned after save");

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync(); // save to generate Issue.Id

            // handle files (if any)
            if (files != null && files.Any())
            {
                _logger.LogInformation($"IssueController.Create - Processing {files.Count} files for IssueId: {issue.Id}");

                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "issues", issue.Id.ToString());
                Directory.CreateDirectory(uploadsRoot);

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;
                    if (file.Length > _maxFileSize)
                    {
                        // optionally delete already saved files and issue if you want atomic behavior
                        ModelState.AddModelError("", $"File {file.FileName} exceeds the allowed size of {_maxFileSize} bytes.");
                        continue;
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_permittedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", $"File '{file.FileName}' type {ext} is not allowed.");
                        continue;
                    }

                    var unique = Guid.NewGuid().ToString("N") + ext;

                    // relative URL under wwwroot (use forward slashes)
                    var relative = Path.Combine("uploads", "issues", issue.Id.ToString(), unique).Replace("\\", "/");
                    var savePath = Path.Combine(_env.WebRootPath, relative.Replace("/", Path.DirectorySeparatorChar.ToString()));

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var record = new FileRecord
                    {
                        IssueId = issue.Id,
                        FileName = file.FileName,
                        FilePath = savePath,
                        FileUrl = "/" + relative,
                        UploadedById = userId,
                        UploadedAt = DateTime.UtcNow
                    };

                    _context.FileRecords.Add(record);
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Issue created successfully.";
            return RedirectToAction(nameof(MyIssues));
        }

        // GET: /Issue/Details/5
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation($"IssueController.Details called - IssueId: {id}");

            var issue = await _context.Issues
                .Include(i => i.Reporter)
                .Include(i => i.Category)
                .Include(i => i.Files)
                .Include(i => i.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();

            // Any authenticated user can view; if you want to restrict, check here

            return View(issue);
        }

        // POST: /Issue/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int issueId, string content)
        {
            _logger.LogInformation($"IssueController.AddComment called - IssueId: {issueId}, ContentLength: {content?.Length}");

            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Details), new { id = issueId });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            var issueExists = await _context.Issues.AnyAsync(i => i.Id == issueId);
            if (!issueExists) return NotFound();

            var comment = new Comment
            {
                IssueId = issueId,
                Content = content.Trim(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // optionally log to AuditLogs

            return RedirectToAction(nameof(Index), new { id = issueId });
        }

        // GET: /Issue/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"IssueController.Edit GET called - IssueId: {id}");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var issue = await _context.Issues.Include(i => i.Files).FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null) return NotFound();

            // only reporter can edit
            if (issue.ReporterId != userId)
                return Forbid();

            ViewBag.Categories = await _context.IssueCategories.OrderBy(c => c.Name).ToListAsync();
            return View(issue);
        }

        // POST: /Issue/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,CategoryId")] Issue model, List<IFormFile> files)
        {
            _logger.LogInformation($"IssueController.Edit POST called - IssueId: {id}, Title: {model.Title}, FileCount: {files?.Count}");

            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.IssueCategories.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var issue = await _context.Issues.Include(i => i.Files).FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null) return NotFound();
            if (issue.ReporterId != userId) return Forbid();

            issue.Title = model.Title;
            issue.Description = model.Description;
            issue.CategoryId = model.CategoryId;
            issue.UpdatedAt = DateTime.UtcNow;

            _context.Issues.Update(issue);
            await _context.SaveChangesAsync();

            // handle new files same as in Create
            if (files != null && files.Any())
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "issues", issue.Id.ToString());
                Directory.CreateDirectory(uploadsRoot);

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;
                    if (file.Length > _maxFileSize) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_permittedExtensions.Contains(ext)) continue;

                    var unique = Guid.NewGuid().ToString("N") + ext;
                    var relative = Path.Combine("uploads", "issues", issue.Id.ToString(), unique).Replace("\\", "/");
                    var savePath = Path.Combine(_env.WebRootPath, relative.Replace("/", Path.DirectorySeparatorChar.ToString()));

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var record = new FileRecord
                    {
                        IssueId = issue.Id,
                        FileName = file.FileName,
                        FilePath = savePath,
                        FileUrl = "/" + relative,
                        UploadedById = userId,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.FileRecords.Add(record);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Issue updated.";
            return RedirectToAction(nameof(MyIssues));
        }

        // GET: /Issue/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"IssueController.Delete GET called - IssueId: {id}");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var issue = await _context.Issues.Include(i => i.Files).FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null) return NotFound();
            if (issue.ReporterId != userId) return Forbid();

            return View(issue);
        }

        // POST: /Issue/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation($"IssueController.Delete POST called - IssueId: {id}");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            var issue = await _context.Issues.Include(i => i.Files).FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null) return NotFound();
            if (issue.ReporterId != userId) return Forbid();

            // delete files from disk
            if (issue.Files != null)
            {
                foreach (var f in issue.Files.ToList())
                {
                    try
                    {
                        if (System.IO.File.Exists(f.FilePath))
                            System.IO.File.Delete(f.FilePath);
                    }
                    catch { /* log error */ }

                    _context.FileRecords.Remove(f);
                }
            }

            // cascade delete comments (configured in OnModelCreating)
            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Issue deleted.";
            return RedirectToAction(nameof(MyIssues));
        }

        // // GET: /Issue/DownloadFile/5
        // [HttpGet]
        // public async Task<IActionResult> DownloadFile(int id)
        // {
        //     var file = await _context.FileRecords.Include(f => f.Issue).FirstOrDefaultAsync(f => f.Id == id);
        //     if (file == null) return NotFound();

        //     // allow download to any authenticated user because issues are viewable by students
        //     // if you want to restrict to reporter or admin, add checks:
        //     // var userId = _userManager.GetUserId(User);
        //     // if (file.Issue.ReporterId != userId && !User.IsInRole("Admin")) return Forbid();

        //     if (!System.IO.File.Exists(file.FilePath)) return NotFound();

        //     var contentType = "application/octet-stream";
        //     return PhysicalFile(file.FilePath, contentType, file.FileName);
        // }

        // Optionally: Remove a single attached file (only reporter can remove)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFile(int id)
        {
            _logger.LogInformation($"IssueController.RemoveFile called - FileRecordId: {id}");

            var file = await _context.FileRecords.Include(f => f.Issue).FirstOrDefaultAsync(f => f.Id == id);
            if (file == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();
            if (file.Issue!.ReporterId != userId) return Forbid();

            try { if (System.IO.File.Exists(file.FilePath)) System.IO.File.Delete(file.FilePath); } catch { /* log */ }

            _context.FileRecords.Remove(file);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = file.IssueId });
        }
    }
}
