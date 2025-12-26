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
            // Optimisé: AsNoTracking pour lecture seule
            var courses = await _context.Courses
                .AsNoTracking()
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
            // Optimisé: AsNoTracking + WHERE précoce
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .FirstOrDefaultAsync();
                
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
    Console.WriteLine($"=== CreateLesson POST called ===");
    Console.WriteLine($"ModuleId: {lesson.ModuleId}");
    Console.WriteLine($"Title: {lesson.Title}");
    Console.WriteLine($"Description length: {lesson.Description?.Length ?? 0}");
    Console.WriteLine($"OrderIndex: {lesson.OrderIndex}");
    Console.WriteLine($"VideoFile: {videoFile?.FileName ?? "null"}");
    
    // Vérification module valide
    var module = await _context.Modules
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == lesson.ModuleId);

    if (module == null)
    {
        Console.WriteLine("ERROR: Module not found");
        ModelState.AddModelError("", "Module introuvable.");
        // On renvoie le ModuleId pour que la vue ait le hidden correct
        ViewBag.ModuleId = lesson.ModuleId;
        return View(lesson);
    }

    // Remove Description validation error if it exists (Quill handles it)
    ModelState.Remove("Description");
    
    // Remove videoFile validation error (video is optional)
    ModelState.Remove("videoFile");

    // Debug: Log validation errors
    if (!ModelState.IsValid)
    {
        Console.WriteLine("ERROR: ModelState is invalid");
        var errors = ModelState
            .Where(x => x.Value!.Errors.Count > 0)
            .Select(x => new { x.Key, x.Value!.Errors })
            .ToArray();
        
        foreach (var error in errors)
        {
            Console.WriteLine($"Validation Error - Field: {error.Key}");
            foreach (var err in error.Errors)
            {
                Console.WriteLine($"  Message: {err.ErrorMessage}");
            }
        }
        
        ViewBag.ModuleId = lesson.ModuleId;
        return View(lesson);
    }

    Console.WriteLine("Validation passed, proceeding with save");

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

    Console.WriteLine("Adding lesson to context");
    _context.Lessons.Add(lesson);
    
    Console.WriteLine("Saving changes to database");
    await _context.SaveChangesAsync();
    
    Console.WriteLine($"Lesson created successfully with ID: {lesson.Id}");

    // Redirection vers le détail du cours
    return RedirectToAction(nameof(Details), new { id = module.CourseId });
}

        // -----------------------------
        // EDIT LESSON
        // -----------------------------
        public async Task<IActionResult> EditLesson(int lessonId)
        {
            // Optimisé: AsNoTracking pour lecture seule lors de l'affichage du formulaire
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound("Leçon introuvable.");

            ViewBag.ModuleId = lesson.ModuleId;
            ViewBag.CourseId = lesson.Module?.CourseId;

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Lesson lesson, IFormFile videoFile)
        {
            var existingLesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lesson.Id);

            if (existingLesson == null)
                return NotFound("Leçon introuvable.");

            // Remove Description validation error if it exists (Quill handles it)
            ModelState.Remove("Description");

            if (!ModelState.IsValid)
            {
                ViewBag.ModuleId = existingLesson.ModuleId;
                ViewBag.CourseId = existingLesson.Module?.CourseId;
                return View(lesson);
            }

            // Upload new video if provided
            if (videoFile != null && videoFile.Length > 0)
            {
                var allowed = new[] { ".mp4", ".webm", ".ogg", ".mp3" };
                var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("videoFile", "Formats autorisés : MP4 / WebM / OGG / MP3");
                    ViewBag.ModuleId = existingLesson.ModuleId;
                    ViewBag.CourseId = existingLesson.Module?.CourseId;
                    return View(lesson);
                }

                if (videoFile.Length > 200L * 1024 * 1024)
                {
                    ModelState.AddModelError("videoFile", "Fichier trop volumineux (max 200 MB).");
                    ViewBag.ModuleId = existingLesson.ModuleId;
                    ViewBag.CourseId = existingLesson.Module?.CourseId;
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

                // Delete old video if exists
                if (!string.IsNullOrEmpty(existingLesson.VideoPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", existingLesson.VideoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                existingLesson.VideoFileName = videoFile.FileName;
                existingLesson.VideoPath = "/videos/" + fileName;
            }

            existingLesson.Title = lesson.Title;
            existingLesson.Description = lesson.Description;
            existingLesson.OrderIndex = lesson.OrderIndex;

            _context.Lessons.Update(existingLesson);
            await _context.SaveChangesAsync();

            var courseId = existingLesson.Module?.CourseId ?? 0;
            if (courseId > 0)
            {
                return RedirectToAction(nameof(Details), new { id = courseId });
            }

            return RedirectToAction(nameof(Index));
        }

        // -----------------------------
        // DELETE LESSON
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound("Leçon introuvable.");

            var courseId = lesson.Module?.CourseId ?? 0;

            // Delete video file if exists
            if (!string.IsNullOrEmpty(lesson.VideoPath))
            {
                var videoPath = Path.Combine(_env.WebRootPath ?? "wwwroot", lesson.VideoPath.TrimStart('/'));
                if (System.IO.File.Exists(videoPath))
                {
                    System.IO.File.Delete(videoPath);
                }
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            if (courseId > 0)
            {
                return RedirectToAction(nameof(Details), new { id = courseId });
            }

            return RedirectToAction(nameof(Index));
        }

        // ========================================
        // QUIZ MANAGEMENT
        // ========================================

        /// <summary>
        /// Affiche le formulaire de création d'un quiz
        /// </summary>
        public async Task<IActionResult> CreateQuiz(int lessonId)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound("Leçon introuvable.");

            ViewBag.LessonId = lessonId;
            ViewBag.Lesson = lesson;
            return View();
        }

        /// <summary>
        /// Crée un nouveau quiz avec ses options
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(int lessonId, Quiz quiz, string[] optionTexts, bool[] optionCorrects)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                ModelState.AddModelError("", "Leçon introuvable.");
                ViewBag.LessonId = lessonId;
                return View();
            }

            if (!ModelState.IsValid || string.IsNullOrEmpty(quiz.Question))
            {
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            // Validation: au moins 2 options avec 1 correcte
            if (optionTexts == null || optionTexts.Length < 2)
            {
                ModelState.AddModelError("", "Minimum 2 options requises.");
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            if (!optionCorrects.Any(c => c))
            {
                ModelState.AddModelError("", "Au moins une option correcte requise.");
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            quiz.LessonId = lessonId;
            quiz.CreatedAt = DateTime.UtcNow;
            quiz.Points = quiz.Points > 0 ? quiz.Points : 1;

            // Ajouter les options
            for (int i = 0; i < optionTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(optionTexts[i]))
                {
                    quiz.Options.Add(new QuizOption
                    {
                        Text = optionTexts[i].Trim(),
                        IsCorrect = i < optionCorrects.Length && optionCorrects[i]
                    });
                }
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            if (lesson?.Module?.CourseId > 0)
            {
                return RedirectToAction("Details", "AdminCourses", new { id = lesson.Module.CourseId });
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Affiche le formulaire d'édition d'un quiz
        /// </summary>
        public async Task<IActionResult> EditQuiz(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Options)
                .Include(q => q.Lesson)
                .ThenInclude(l => l!.Module)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return NotFound("Quiz introuvable.");

            ViewBag.Lesson = quiz.Lesson!;
            return View(quiz);
        }

        /// <summary>
        /// Met à jour un quiz existant
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuiz(int quizId, Quiz quiz, string[] optionTexts, bool[] optionCorrects)
        {
            var existingQuiz = await _context.Quizzes
                .Include(q => q.Options)
                .Include(q => q.Lesson)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (existingQuiz == null)
                return NotFound("Quiz introuvable.");

            if (string.IsNullOrEmpty(quiz.Question))
            {
                ModelState.AddModelError("", "La question est requise.");
                ViewBag.Lesson = existingQuiz.Lesson;
                return View(existingQuiz);
            }

            if (optionTexts == null || optionTexts.Length < 2)
            {
                ModelState.AddModelError("", "Minimum 2 options requises.");
                ViewBag.Lesson = existingQuiz.Lesson;
                return View(existingQuiz);
            }

            if (!optionCorrects.Any(c => c))
            {
                ModelState.AddModelError("", "Au moins une option correcte requise.");
                ViewBag.Lesson = existingQuiz.Lesson;
                return View(existingQuiz);
            }

            existingQuiz.Question = quiz.Question;
            existingQuiz.Description = quiz.Description;
            existingQuiz.Points = quiz.Points > 0 ? quiz.Points : 1;

            // Supprimer les anciennes options
            _context.QuizOptions.RemoveRange(existingQuiz.Options);

            // Ajouter les nouvelles options
            for (int i = 0; i < optionTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(optionTexts[i]))
                {
                    existingQuiz.Options.Add(new QuizOption
                    {
                        Text = optionTexts[i].Trim(),
                        IsCorrect = i < optionCorrects.Length && optionCorrects[i]
                    });
                }
            }

            _context.Quizzes.Update(existingQuiz);
            await _context.SaveChangesAsync();

            if (existingQuiz?.Lesson?.Module?.CourseId > 0)
            {
                return RedirectToAction("Details", "AdminCourses", new { id = existingQuiz.Lesson.Module.CourseId });
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Supprime un quiz
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l!.Module)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return NotFound("Quiz introuvable.");

            var courseId = quiz.Lesson!.Module!.CourseId;

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            if (courseId > 0)
            {
                return RedirectToAction("Details", "AdminCourses", new { id = courseId });
            }
            
            return RedirectToAction("Index");
        }

        // -----------------------------
        // USERS MANAGEMENT
        // -----------------------------
        public async Task<IActionResult> Users()
        {
            // Optimisé: AsNoTracking pour lecture seule
            var users = await _context.Users.AsNoTracking().ToListAsync();
            var subscriptions = await _context.Subscriptions.AsNoTracking().ToListAsync();
            
            var userSubscriptions = users.Select(user => new
            {
                User = user,
                Subscription = subscriptions.FirstOrDefault(s => s.UserId == user.Email && s.IsActive)
            }).ToList();
            
            ViewBag.UserSubscriptions = userSubscriptions;
            return View();
        }

    }
}
