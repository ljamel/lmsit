using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public AdminCoursesController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
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
                .ThenInclude(l => l.Quizzes)
                .ThenInclude(q => q.Options)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
                
            if (course == null) return NotFound();
            return View(course);
        }

        public async Task<IActionResult> CreateModule(int courseId)
        {
            // Verify the course exists
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);
            
            if (course == null)
            {
                return NotFound("Course not found.");
            }
            
            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course.Title;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModule(Module module)
        {
            Console.WriteLine($"=== CreateModule POST called ===");
            Console.WriteLine($"CourseId: {module.CourseId}");
            Console.WriteLine($"Title: {module.Title}");
            Console.WriteLine($"Description: {module.Description}");
            Console.WriteLine($"OrderIndex: {module.OrderIndex}");

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
                
                ViewBag.CourseId = module.CourseId;
                return View(module);
            }

            module.CreatedAt = DateTime.UtcNow;
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Module created successfully with ID: {module.Id}");
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
public async Task<IActionResult> CreateLesson(Lesson lesson)
{
    Console.WriteLine($"=== CreateLesson POST called ===");
    Console.WriteLine($"ModuleId: {lesson.ModuleId}");
    Console.WriteLine($"Title: {lesson.Title}");
    Console.WriteLine($"Description length: {lesson.Description?.Length ?? 0}");
    Console.WriteLine($"OrderIndex: {lesson.OrderIndex}");
    
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
            Console.WriteLine($"[EditLesson GET] Called with lessonId={lessonId}");
            
            // Optimisé: AsNoTracking pour lecture seule lors de l'affichage du formulaire
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                Console.WriteLine($"[EditLesson GET] Lesson not found with ID={lessonId}");
                return NotFound("Leçon introuvable.");
            }

            Console.WriteLine($"[EditLesson GET] Lesson found: {lesson.Title}");
            ViewBag.ModuleId = lesson.ModuleId;
            ViewBag.CourseId = lesson.Module?.CourseId;

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Lesson lesson)
        {
            Console.WriteLine($"[EditLesson POST] Called with LessonId={lesson.Id}");
            Console.WriteLine($"[EditLesson POST] Title={lesson.Title}");
            Console.WriteLine($"[EditLesson POST] Description length={lesson.Description?.Length ?? 0}");
            Console.WriteLine($"[EditLesson POST] OrderIndex={lesson.OrderIndex}");
            
            var existingLesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lesson.Id);

            if (existingLesson == null)
            {
                Console.WriteLine($"[EditLesson POST] Lesson not found with ID={lesson.Id}");
                return NotFound("Leçon introuvable.");
            }

            Console.WriteLine($"[EditLesson POST] Existing lesson found: {existingLesson.Title}");
            Console.WriteLine($"[EditLesson POST] Existing description length={existingLesson.Description?.Length ?? 0}");

            // Remove Description validation error if it exists (Quill handles it)
            ModelState.Remove("Description");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[EditLesson POST] ModelState is invalid:");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Count > 0)
                    {
                        Console.WriteLine($"  - {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
                ViewBag.ModuleId = existingLesson.ModuleId;
                ViewBag.CourseId = existingLesson.Module?.CourseId;
                return View(lesson);
            }

            existingLesson.Title = lesson.Title;
            existingLesson.Description = lesson.Description ?? "";
            existingLesson.OrderIndex = lesson.OrderIndex;

            Console.WriteLine($"[EditLesson POST] Updating lesson with new values:");
            Console.WriteLine($"[EditLesson POST] New Title={existingLesson.Title}");
            Console.WriteLine($"[EditLesson POST] New Description length={existingLesson.Description?.Length ?? 0}");
            if (!string.IsNullOrEmpty(existingLesson.Description))
            {
                Console.WriteLine($"[EditLesson POST] New Description preview={existingLesson.Description.Substring(0, Math.Min(100, existingLesson.Description.Length))}");
            }

            _context.Lessons.Update(existingLesson);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("[EditLesson POST] Changes saved successfully");

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
                .Include(l => l.Module)
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
        public async Task<IActionResult> CreateQuiz(int lessonId, IFormCollection form)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                ModelState.AddModelError("", "Leçon introuvable.");
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = null;
                return View();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            var questionTexts = form["questionTexts"].ToArray();
            var questionDescriptions = form["questionDescriptions"].ToArray();
            var questionPoints = form["questionPoints"].ToArray();

            if (questionTexts.Length == 0)
            {
                ModelState.AddModelError("", "Ajoutez au moins une question.");
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            var quizzesToCreate = new System.Collections.Generic.List<Quiz>();

            for (int questionIndex = 0; questionIndex < questionTexts.Length; questionIndex++)
            {
                var questionText = questionTexts[questionIndex]?.Trim();
                if (string.IsNullOrWhiteSpace(questionText))
                {
                    continue;
                }

                var optionTextsForQuestion = form[$"optionTexts_{questionIndex}"]
                    .Select(o => o?.Trim())
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .ToList();

                if (optionTextsForQuestion.Count < 2)
                {
                    ModelState.AddModelError("", $"La question #{questionIndex + 1} doit avoir au moins 2 options.");
                    ViewBag.LessonId = lessonId;
                    ViewBag.Lesson = lesson;
                    return View();
                }

                var correctIndexesRaw = form[$"correctIndexes_{questionIndex}"].FirstOrDefault() ?? string.Empty;
                var correctIndexes = correctIndexesRaw
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => int.TryParse(value, out var idx) ? idx : -1)
                    .Where(idx => idx >= 0 && idx < optionTextsForQuestion.Count)
                    .Distinct()
                    .ToHashSet();

                if (correctIndexes.Count == 0)
                {
                    ModelState.AddModelError("", $"La question #{questionIndex + 1} doit avoir au moins une réponse correcte.");
                    ViewBag.LessonId = lessonId;
                    ViewBag.Lesson = lesson;
                    return View();
                }

                var points = 1;
                if (questionIndex < questionPoints.Length && int.TryParse(questionPoints[questionIndex], out var parsedPoints) && parsedPoints > 0)
                {
                    points = parsedPoints;
                }

                var description = questionIndex < questionDescriptions.Length
                    ? questionDescriptions[questionIndex]
                    : null;

                var quiz = new Quiz
                {
                    LessonId = lessonId,
                    Question = questionText,
                    Description = description,
                    Points = points,
                    CreatedAt = DateTime.UtcNow
                };

                for (int optionIndex = 0; optionIndex < optionTextsForQuestion.Count; optionIndex++)
                {
                    quiz.Options.Add(new QuizOption
                    {
                        Text = optionTextsForQuestion[optionIndex]!,
                        IsCorrect = correctIndexes.Contains(optionIndex)
                    });
                }

                quizzesToCreate.Add(quiz);
            }

            if (quizzesToCreate.Count == 0)
            {
                ModelState.AddModelError("", "Ajoutez au moins une question valide.");
                ViewBag.LessonId = lessonId;
                ViewBag.Lesson = lesson;
                return View();
            }

            _context.Quizzes.AddRange(quizzesToCreate);
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
        private sealed class UserTrackingSummary
        {
            public int CoursesTracked { get; set; }
            public int QuizAttempts { get; set; }
            public int QuizCorrectAnswers { get; set; }
            public int QuizLessonsTracked { get; set; }
            public int CommentsCount { get; set; }
            public DateTime? LastActivityAt { get; set; }
        }

        public async Task<IActionResult> Users(string? search, int page = 1)
        {
            const int pageSize = 15;
            if (page < 1)
            {
                page = 1;
            }

            var activeSubscriptions = await _context.Subscriptions
                .Where(s => s.IsActive)
                .ToListAsync();

            await SyncStripeSubscriptionsAsync(activeSubscriptions);

            var filteredUsersQuery = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                filteredUsersQuery = filteredUsersQuery.Where(u =>
                    (u.Email != null && EF.Functions.Like(u.Email, searchTerm)) ||
                    (u.UserName != null && EF.Functions.Like(u.UserName, searchTerm))
                );
            }

            var activeUserIdsQuery = _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => s.UserId)
                .Distinct();

            var latestSubscriptionDateByUserQuery = _context.Subscriptions
                .AsNoTracking()
                .GroupBy(s => s.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    LatestSubscriptionStartDate = group.Max(s => s.StartDate)
                });

            var totalUsers = await filteredUsersQuery.CountAsync();
            var totalActiveSubscriptions = await filteredUsersQuery
                .Where(u => u.Email != null && activeUserIdsQuery.Contains(u.Email))
                .CountAsync();

            var totalPages = Math.Max(1, (int)Math.Ceiling(totalUsers / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var users = await (
                from user in filteredUsersQuery
                join latestSubscription in latestSubscriptionDateByUserQuery
                    on user.Email equals latestSubscription.UserId into latestSubscriptionJoin
                from latestSubscription in latestSubscriptionJoin.DefaultIfEmpty()
                orderby (user.Email != null && activeUserIdsQuery.Contains(user.Email)) descending,
                    latestSubscription.LatestSubscriptionStartDate descending,
                    user.Id descending
                select user
            )
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedUserIds = users.Select(user => user.Id).ToList();

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            var quizActivityRows = await (
                from result in _context.UserQuizResults.AsNoTracking()
                join quiz in _context.Quizzes.AsNoTracking() on result.QuizId equals quiz.Id
                join lesson in _context.Lessons.AsNoTracking() on quiz.LessonId equals lesson.Id
                join module in _context.Modules.AsNoTracking() on lesson.ModuleId equals module.Id
                select new
                {
                    result.UserId,
                    result.IsCorrect,
                    result.AttemptedAt,
                    LessonId = lesson.Id,
                    CourseId = module.CourseId
                }
            ).ToListAsync();

            var commentActivityRows = await _context.Comments
                .AsNoTracking()
                .Select(comment => new
                {
                    comment.UserId,
                    comment.CourseId,
                    comment.CreatedAt
                })
                .ToListAsync();

            var quizByUserId = quizActivityRows
                .GroupBy(row => row.UserId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var commentsByUserEmail = commentActivityRows
                .GroupBy(row => row.UserId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var quizStatsByUserId = quizByUserId.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    QuizAttempts = pair.Value.Count,
                    QuizCorrectAnswers = pair.Value.Count(row => row.IsCorrect),
                    QuizLessonsTracked = pair.Value.Select(row => row.LessonId).Distinct().Count(),
                    CourseIds = pair.Value.Select(row => row.CourseId).Distinct().ToHashSet(),
                    LastQuizActivityAt = pair.Value.Max(row => row.AttemptedAt)
                });

            var commentStatsByUserEmail = commentsByUserEmail.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    CommentsCount = pair.Value.Count,
                    CourseIds = pair.Value.Select(row => row.CourseId).Distinct().ToHashSet(),
                    LastCommentActivityAt = pair.Value.Max(row => row.CreatedAt)
                });
            
            var userRows = users.Select(user =>
            {
                var subscription = subscriptions.FirstOrDefault(s => s.UserId == user.Email && s.IsActive);
                var tracking = BuildTrackingForUser(user.Id, user.Email);
                var sortDate = subscription?.StartDate ?? tracking.LastActivityAt;

                return new AdminUserRowViewModel
                {
                    UserId = user.Email ?? user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName,
                    HasSubscription = subscription != null,
                    IsActiveSubscription = subscription?.IsActive ?? false,
                    SubscriptionStatus = subscription?.Status,
                    SubscriptionStartDate = subscription?.StartDate,
                    StripeSubscriptionId = subscription?.StripeSubscriptionId,
                    CoursesTracked = tracking.CoursesTracked,
                    QuizAttempts = tracking.QuizAttempts,
                    QuizCorrectAnswers = tracking.QuizCorrectAnswers,
                    QuizLessonsTracked = tracking.QuizLessonsTracked,
                    CommentsCount = tracking.CommentsCount,
                    LastActivityAt = tracking.LastActivityAt,
                    SortDate = sortDate
                };
            })
            .OrderByDescending(row => row.SortDate)
            .ThenBy(row => row.Email)
            .ToList();

            var quizCompetencyRowsRaw = await (
                from result in _context.UserQuizResults.AsNoTracking()
                join user in _context.Users.AsNoTracking() on result.UserId equals user.Id
                join quiz in _context.Quizzes.AsNoTracking() on result.QuizId equals quiz.Id
                join lesson in _context.Lessons.AsNoTracking() on quiz.LessonId equals lesson.Id
                join module in _context.Modules.AsNoTracking() on lesson.ModuleId equals module.Id
                join course in _context.Courses.AsNoTracking() on module.CourseId equals course.Id
                where pagedUserIds.Contains(result.UserId)
                group new { result, user, quiz, lesson, course } by new
                {
                    result.UserId,
                    user.Email,
                    user.UserName,
                    QuizId = quiz.Id,
                    QuizQuestion = quiz.Question,
                    LessonTitle = lesson.Title,
                    CourseTitle = course.Title
                }
                into grouped
                select new
                {
                    grouped.Key.UserId,
                    grouped.Key.Email,
                    grouped.Key.UserName,
                    grouped.Key.QuizId,
                    grouped.Key.QuizQuestion,
                    grouped.Key.LessonTitle,
                    grouped.Key.CourseTitle,
                    Attempts = grouped.Count(),
                    CorrectAnswers = grouped.Count(x => x.result.IsCorrect),
                    LastAttemptAt = grouped.Max(x => x.result.AttemptedAt)
                }
            )
            .ToListAsync();

            const string orientationClaimType = "cyber_orientation_result";
            var orientationClaims = await _context.UserClaims
                .AsNoTracking()
                .Where(claim => pagedUserIds.Contains(claim.UserId) && claim.ClaimType == orientationClaimType)
                .ToListAsync();

            var orientationRoleByUserId = orientationClaims
                .GroupBy(claim => claim.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => ExtractOrientationRole(group
                        .OrderByDescending(claim => claim.Id)
                        .Select(claim => claim.ClaimValue)
                        .FirstOrDefault()));

            var quizCompetencyRows = quizCompetencyRowsRaw
                .Select(row =>
                {
                    var successRate = row.Attempts == 0 ? 0 : (row.CorrectAnswers * 100.0) / row.Attempts;
                    orientationRoleByUserId.TryGetValue(row.UserId, out var orientationRole);

                    return new AdminUserQuizResultRowViewModel
                    {
                        UserId = row.UserId,
                        UserEmail = row.Email ?? string.Empty,
                        UserName = row.UserName,
                        OrientationRole = orientationRole,
                        QuizId = row.QuizId,
                        QuizQuestion = row.QuizQuestion,
                        LessonTitle = row.LessonTitle,
                        CourseTitle = row.CourseTitle,
                        Attempts = row.Attempts,
                        CorrectAnswers = row.CorrectAnswers,
                        SuccessRate = Math.Round(successRate, 1),
                        LastAttemptAt = row.LastAttemptAt,
                        CompetencyLevel = GetCompetencyLevel(successRate)
                    };
                })
                .OrderBy(row => row.UserEmail)
                .ThenByDescending(row => row.LastAttemptAt)
                .ThenBy(row => row.QuizId)
                .ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveSubscriptions = totalActiveSubscriptions;
            ViewBag.WithoutSubscription = totalUsers - totalActiveSubscriptions;
            ViewBag.SearchQuery = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.QuizCompetencyRows = quizCompetencyRows;

            return View(userRows);

            UserTrackingSummary BuildTrackingForUser(string userId, string? userEmail)
            {
                quizStatsByUserId.TryGetValue(userId, out var quizStats);
                commentStatsByUserEmail.TryGetValue(userEmail ?? string.Empty, out var commentStats);

                var courseIds = new System.Collections.Generic.HashSet<int>();
                if (quizStats != null)
                {
                    courseIds.UnionWith(quizStats.CourseIds);
                }

                if (commentStats != null)
                {
                    courseIds.UnionWith(commentStats.CourseIds);
                }

                var lastActivityAt = new[]
                {
                    quizStats != null ? (DateTime?)quizStats.LastQuizActivityAt : null,
                    commentStats != null ? (DateTime?)commentStats.LastCommentActivityAt : null
                }
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .DefaultIfEmpty()
                .Max();

                DateTime? normalizedLastActivity = lastActivityAt == default ? null : lastActivityAt;

                return new UserTrackingSummary
                {
                    CoursesTracked = courseIds.Count,
                    QuizAttempts = quizStats?.QuizAttempts ?? 0,
                    QuizCorrectAnswers = quizStats?.QuizCorrectAnswers ?? 0,
                    QuizLessonsTracked = quizStats?.QuizLessonsTracked ?? 0,
                    CommentsCount = commentStats?.CommentsCount ?? 0,
                    LastActivityAt = normalizedLastActivity
                };
            }

            static string GetCompetencyLevel(double successRate)
            {
                if (successRate >= 80)
                {
                    return "Avancé";
                }

                if (successRate >= 50)
                {
                    return "Intermédiaire";
                }

                if (successRate > 0)
                {
                    return "Débutant";
                }

                return "Non évalué";
            }

            static string? ExtractOrientationRole(string? claimValue)
            {
                if (string.IsNullOrWhiteSpace(claimValue))
                {
                    return null;
                }

                try
                {
                    using var document = JsonDocument.Parse(claimValue);
                    if (document.RootElement.TryGetProperty("role", out var roleElement))
                    {
                        var role = roleElement.GetString();
                        return string.IsNullOrWhiteSpace(role) ? null : role;
                    }
                }
                catch (JsonException)
                {
                }

                return null;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateMember(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(nameof(Users));
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                TempData["Error"] = "Clé Stripe manquante. Annulation impossible.";
                return RedirectToAction(nameof(Users));
            }

            StripeConfiguration.ApiKey = secretKey;
            var stripeService = new Stripe.SubscriptionService();

            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (subscriptions.Count == 0)
            {
                TempData["Error"] = "Aucun abonnement actif à désactiver.";
                return RedirectToAction(nameof(Users));
            }

            var now = DateTime.UtcNow;
            var stripeErrors = false;
            foreach (var subscription in subscriptions)
            {
                if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
                {
                    try
                    {
                        await stripeService.CancelAsync(subscription.StripeSubscriptionId);
                    }
                    catch (StripeException ex)
                    {
                        stripeErrors = true;
                        Console.WriteLine($"Stripe cancel failed for subscription {subscription.Id}: {ex.Message}");
                    }
                }

                subscription.IsActive = false;
                subscription.Status = "canceled";
                subscription.CanceledAt = now;
                subscription.EndDate = now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = stripeErrors
                ? "Abonnement désactivé en base, mais l'annulation Stripe a échoué."
                : "Abonnement désactivé et annulé sur Stripe.";
            return RedirectToAction(nameof(Users));
        }

        private async Task SyncStripeSubscriptionsAsync(IEnumerable<CrudDemo.Models.Subscription> activeSubscriptions)
        {
            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return;
            }

            StripeConfiguration.ApiKey = secretKey;
            var stripeService = new Stripe.SubscriptionService();

            foreach (var subscription in activeSubscriptions)
            {
                if (string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
                {
                    continue;
                }

                try
                {
                    var stripeSubscription = await stripeService.GetAsync(subscription.StripeSubscriptionId);
                    var isStripeActive = stripeSubscription != null
                        && (stripeSubscription.Status == "active" || stripeSubscription.Status == "trialing");

                    if (!isStripeActive)
                    {
                        subscription.IsActive = false;
                        subscription.Status = stripeSubscription?.Status ?? "inactive";
                        subscription.EndDate = DateTime.UtcNow;

                        if (stripeSubscription?.CanceledAt != null)
                        {
                            subscription.CanceledAt = stripeSubscription.CanceledAt.Value;
                        }
                    }
                }
                catch (StripeException ex)
                {
                    if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        subscription.IsActive = false;
                        subscription.Status = "not_found";
                        subscription.EndDate = DateTime.UtcNow;
                    }

                    Console.WriteLine($"Stripe check failed for subscription {subscription.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
