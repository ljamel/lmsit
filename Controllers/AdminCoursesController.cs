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
        public async Task<IActionResult> CreateModule([Bind("CourseId,Title,Description,OrderIndex")] Module module)
        {
            if (!ModelState.IsValid) return View(module);
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = module.CourseId });
        }

        public IActionResult CreateLesson(int moduleId)
        {
            ViewBag.ModuleId = moduleId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson([Bind("ModuleId,Title,Description,OrderIndex")] Lesson lesson, IFormFile videoFile)
        {
            if (!ModelState.IsValid) return View(lesson);

            if (videoFile != null && videoFile.Length > 0)
            {
                var allowed = new[] { ".mp4", ".webm", ".ogg", ".mp3" };
                var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("videoFile", "Only MP4/WebM/OGG/MP3 files are allowed.");
                    return View(lesson);
                }

                const long maxBytes = 200L * 1024L * 1024L; // 200 MB
                if (videoFile.Length > maxBytes)
                {
                    ModelState.AddModelError("videoFile", "File too large (max 200 MB).");
                    return View(lesson);
                }

                var videosPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "videos");
                if (!Directory.Exists(videosPath)) Directory.CreateDirectory(videosPath);
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var savePath = Path.Combine(videosPath, fileName);
                using (var stream = System.IO.File.Create(savePath))
                {
                    await videoFile.CopyToAsync(stream);
                }

                lesson.VideoFileName = videoFile.FileName;
                lesson.VideoPath = "/videos/" + fileName;
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            // redirect back to course details
            var module = await _context.Modules.FindAsync(lesson.ModuleId);
            return RedirectToAction(nameof(Details), new { id = module?.CourseId });
        }
    }
}
