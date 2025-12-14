using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminCoursesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Modules)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(courses);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description")] Course course)
        {
            if (!ModelState.IsValid) return View(course);
            course.CreatedBy = User?.Identity?.Name ?? "admin";
            course.CreatedAt = DateTime.UtcNow;
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();
            return View(course);
        }

        public IActionResult CreateModule(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(Module module)
        {
            if (!ModelState.IsValid)
                return View(module);

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = module.CourseId });
        }

        // -----------------------------
        // CREATE LESSON
        // -----------------------------
        public async Task<IActionResult> CreateLesson(int moduleId)
        {
            var module = await _context.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return NotFound("Module introuvable.");

            ViewBag.ModuleId = moduleId;
            ViewBag.CourseId = module.CourseId;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
public async Task<IActionResult> CreateLesson(Lesson lesson, IFormFile videoFile)
{
    // Vérification module valide
    var module = await _context.Modules
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == lesson.ModuleId);

    if (module == null)
    {
        ModelState.AddModelError("", "Module introuvable.");
        // On renvoie le ModuleId pour que la vue ait le hidden correct
        ViewBag.ModuleId = lesson.ModuleId;
        return View(lesson);
    }

    if (!ModelState.IsValid)
    {
        ViewBag.ModuleId = lesson.ModuleId;
        return View(lesson);
    }

    // Upload vidéo
    if (videoFile != null && videoFile.Length > 0)
    {
        var allowed = new[] { ".mp4", ".webm", ".ogg", ".mp3" };
        var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();

        if (!allowed.Contains(ext))
        {
            ModelState.AddModelError("videoFile", "Formats autorisés : MP4 / WebM / OGG / MP3");
            ViewBag.ModuleId = lesson.ModuleId;
            return View(lesson);
        }

        if (videoFile.Length > 200L * 1024 * 1024)
        {
            ModelState.AddModelError("videoFile", "Fichier trop volumineux (max 200 MB).");
            ViewBag.ModuleId = lesson.ModuleId;
            return View(lesson);
        }

        var videosPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "videos");
        Directory.CreateDirectory(videosPath);

        var fileName = Guid.NewGuid().ToString("N") + ext;
        var savePath = Path.Combine(videosPath, fileName);

        await using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await videoFile.CopyToAsync(stream);
        }

        lesson.VideoFileName = videoFile.FileName;
        lesson.VideoPath = "/videos/" + fileName;
    }

    _context.Lessons.Add(lesson);
    await _context.SaveChangesAsync();

    // Redirection vers le détail du cours
    return RedirectToAction(nameof(Details), new { id = module.CourseId });
}

    }
}
