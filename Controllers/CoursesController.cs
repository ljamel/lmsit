using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all courses (public view for authenticated users)
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(courses);
        }

        // View course details with modules and lessons
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                    .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        // View a specific lesson and video
        public async Task<IActionResult> Lesson(int id)
        {
            // Load lesson with module (no complex chaining)
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            // Load course separately
            if (lesson.Module != null)
            {
                var course = await _context.Courses.FindAsync(lesson.Module.CourseId);
                lesson.Module.Course = course;
            }

            // Load quizzes separately (without ThenInclude)
            var quizzes = await _context.Quizzes
                .Where(q => q.LessonId == id)
                .ToListAsync();

            // Load quiz options for each quiz
            foreach (var quiz in quizzes)
            {
                var options = await _context.QuizOptions
                    .Where(o => o.QuizId == quiz.Id)
                    .ToListAsync();
                quiz.Options = options;
            }

            lesson.Quizzes = quizzes;

            // Get the current user's previous quiz attempts for this lesson
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var quizIds = quizzes.Select(q => q.Id).ToList();
                var userAttempts = await _context.UserQuizResults
                    .Where(r => r.UserId == userId && quizIds.Contains(r.QuizId))
                    .ToListAsync();

                ViewBag.UserAttempts = userAttempts;
            }

            return View(lesson);
        }

        // Submit a quiz answer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuizAnswer(int quizId, int optionId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            var option = await _context.QuizOptions.FindAsync(optionId);

            if (quiz == null || option == null)
                return BadRequest("Invalid quiz or option.");

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var result = new UserQuizResult
            {
                UserId = userId,
                QuizId = quizId,
                IsCorrect = option.IsCorrect,
                AttemptedAt = DateTime.UtcNow
            };

            _context.UserQuizResults.Add(result);
            await _context.SaveChangesAsync();

            // Redirect back to lesson with a success message
            var lesson = await _context.Lessons.FindAsync(quiz.LessonId);
            if (lesson == null)
                return NotFound();

            return RedirectToAction(nameof(Lesson), new { id = lesson.Id });
        }

        // Show quiz results for a lesson
        public async Task<IActionResult> QuizResults(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound();

            // Load course separately to avoid null reference issues
            var course = await _context.Courses.FindAsync(lesson.Module?.CourseId);
            if (lesson.Module != null)
                lesson.Module.Course = course;

            var quizzes = await _context.Quizzes
                .Where(q => q.LessonId == lessonId)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var results = await _context.UserQuizResults
                .Where(r => r.UserId == userId && quizIds.Contains(r.QuizId))
                .Include(r => r.Quiz)
                .ToListAsync();

            ViewBag.Lesson = lesson;
            ViewBag.CorrectCount = results.Count(r => r.IsCorrect);
            ViewBag.TotalQuestions = quizzes.Count;

            return View(results);
        }
    }
}
