using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CrudDemo.Controllers
{
    public class TutorialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<TutorialsController> _logger;

        public TutorialsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<TutorialsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // -----------------------------------------------------------------
        // PUBLIC
        // -----------------------------------------------------------------

        // GET: /Tutorials  ou  /Tutorials?category=slug
        public async Task<IActionResult> Index(string? category)
        {
            var categories = await _context.TutorialCategories
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var query = _context.Tutorials
                .Include(t => t.Category)
                .Where(t => t.IsPublished);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category!.Slug == category);
            }

            var tutorials = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = category;
            return View(tutorials);
        }

        // GET: /Tutorials/Details/5 (redirige vers l'URL slug si disponible)
        public async Task<IActionResult> Details(int id)
        {
            var tutorial = await _context.Tutorials
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsPublished);

            if (tutorial == null)
                return NotFound();

            if (!string.IsNullOrEmpty(tutorial.Slug))
                return RedirectPermanent($"/tutoriels/{tutorial.Slug}");

            return View(tutorial);
        }

        // GET: /tutoriels/{slug}
        public async Task<IActionResult> DetailsBySlug(string slug)
        {
            var tutorial = await _context.Tutorials
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Slug == slug && t.IsPublished);

            if (tutorial == null)
                return NotFound();

            return View("Details", tutorial);
        }

        // -----------------------------------------------------------------
        // ADMIN — TUTORIALS
        // -----------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var tutorials = await _context.Tutorials
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(tutorials);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.TutorialCategories
                .OrderBy(c => c.OrderIndex).ThenBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Tutorial model)
        {
            if (ModelState.IsValid)
            {
                model.Slug = GenerateUniqueSlug(model.Title);
                model.AuthorId = _userManager.GetUserId(User) ?? string.Empty;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                _context.Tutorials.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tutoriel créé avec succès !";
                return RedirectToAction(nameof(AdminIndex));
            }

            ViewBag.Categories = await _context.TutorialCategories
                .OrderBy(c => c.OrderIndex).ThenBy(c => c.Name).ToListAsync();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var tutorial = await _context.Tutorials.FindAsync(id);
            if (tutorial == null) return NotFound();

            ViewBag.Categories = await _context.TutorialCategories
                .OrderBy(c => c.OrderIndex).ThenBy(c => c.Name).ToListAsync();
            return View(tutorial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Tutorial model)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var existing = await _context.Tutorials.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Title = model.Title;
                existing.Summary = model.Summary;
                existing.Content = model.Content;
                existing.CategoryId = model.CategoryId;
                existing.IsPublished = model.IsPublished;
                existing.ThumbnailUrl = model.ThumbnailUrl;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tutoriel mis à jour !";
                return RedirectToAction(nameof(AdminIndex));
            }

            ViewBag.Categories = await _context.TutorialCategories
                .OrderBy(c => c.OrderIndex).ThenBy(c => c.Name).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var tutorial = await _context.Tutorials.FindAsync(id);
            if (tutorial != null)
            {
                _context.Tutorials.Remove(tutorial);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tutoriel supprimé.";
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        // -----------------------------------------------------------------
        // ADMIN — CATEGORIES
        // -----------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCategories()
        {
            var categories = await _context.TutorialCategories
                .Include(c => c.Tutorials)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory(TutorialCategory model)
        {
            if (ModelState.IsValid)
            {
                model.Slug = GenerateCategorySlug(model.Name);
                _context.TutorialCategories.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Catégorie créée !";
                return RedirectToAction(nameof(AdminCategories));
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.TutorialCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCategory(int id, TutorialCategory model)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var existing = await _context.TutorialCategories.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.OrderIndex = model.OrderIndex;
                existing.Slug = GenerateCategorySlug(model.Name);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Catégorie mise à jour !";
                return RedirectToAction(nameof(AdminCategories));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.TutorialCategories.FindAsync(id);
            if (category != null)
            {
                _context.TutorialCategories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Catégorie supprimée.";
            }
            return RedirectToAction(nameof(AdminCategories));
        }

        // -----------------------------------------------------------------
        // HELPERS
        // -----------------------------------------------------------------

        private static string Slugify(string text)
        {
            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"[àâä]", "a");
            text = Regex.Replace(text, @"[éèêë]", "e");
            text = Regex.Replace(text, @"[îï]", "i");
            text = Regex.Replace(text, @"[ôö]", "o");
            text = Regex.Replace(text, @"[ùûü]", "u");
            text = Regex.Replace(text, @"[ç]", "c");
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"-+", "-");
            return text.Trim('-');
        }

        private string GenerateUniqueSlug(string title)
        {
            var slug = Slugify(title);
            var baseSlug = slug;
            var counter = 1;
            while (_context.Tutorials.Any(t => t.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }
            return slug;
        }

        private string GenerateCategorySlug(string name)
        {
            var slug = Slugify(name);
            return slug;
        }
    }
}
