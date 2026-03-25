using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CrudDemo.Data;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

public class CoursesListViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CoursesListViewComponent> _logger;

    public CoursesListViewComponent(ApplicationDbContext context, ILogger<CoursesListViewComponent> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Optimisé: Async + AsNoTracking pour réduire la consommation CPU
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var courses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.Quizzes)
            .AsSplitQuery()
            .ToListAsync();

        var firstCourseId = courses
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Select(c => c.Id)
            .FirstOrDefault();

        var userEmail = User?.Identity?.Name ?? string.Empty;
        var userId = HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var trackedCourseIds = new HashSet<int>();
        var trackedLessonIds = new HashSet<int>();
        var totalQuizCount = await _context.Quizzes.AsNoTracking().CountAsync();
        var totalQuizPoints = await _context.Quizzes.AsNoTracking().SumAsync(q => q.Points);
        var completedQuizCount = 0;
        var earnedQuizPoints = 0;
        var certificateScorePercent = 0d;
        var isCertificateEligible = false;
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var trackedCourseIdList = await _context.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.UserId == userEmail && e.IsActive)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();

            trackedCourseIds = trackedCourseIdList.ToHashSet();

            try
            {
                var trackedLessonIdList = await _context.LessonEngagements
                    .AsNoTracking()
                    .Where(e => e.UserId == userEmail && e.IsActive)
                    .Select(e => e.LessonId)
                    .Distinct()
                    .ToListAsync();

                trackedLessonIds = trackedLessonIdList.ToHashSet();
            }
            catch (Exception ex) when (IsLessonEngagementsTableMissing(ex))
            {
                _logger.LogWarning(ex, "Table LessonEngagements absente: progression leçons désactivée temporairement.");
                trackedLessonIds = new HashSet<int>();
            }
        }

        var totalLessons = courses
            .SelectMany(c => c.Modules)
            .SelectMany(m => m.Lessons)
            .Select(l => l.Id)
            .Distinct()
            .Count();

        var trackedLessonsCount = trackedLessonIds
            .Count(lessonId => courses
                .SelectMany(c => c.Modules)
                .SelectMany(m => m.Lessons)
                .Any(l => l.Id == lessonId));

        var progressPercent = totalLessons == 0
            ? 0
            : Math.Round((trackedLessonsCount * 100.0) / totalLessons, 0);

        var canAccessKaliSandbox = await HasPaidAccessAsync(userEmail);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var userQuizResults = await _context.UserQuizResults
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => new { r.QuizId, r.IsCorrect, r.AttemptedAt })
                .ToListAsync();

            var latestResultsByQuiz = userQuizResults
                .GroupBy(r => r.QuizId)
                .Select(group => group.OrderByDescending(x => x.AttemptedAt).First())
                .ToList();

            completedQuizCount = latestResultsByQuiz.Count;

            if (latestResultsByQuiz.Count > 0)
            {
                var quizIds = latestResultsByQuiz.Select(x => x.QuizId).Distinct().ToList();
                var quizPointsById = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => quizIds.Contains(q.Id))
                    .Select(q => new { q.Id, q.Points })
                    .ToDictionaryAsync(q => q.Id, q => q.Points);

                earnedQuizPoints = latestResultsByQuiz
                    .Where(x => x.IsCorrect && quizPointsById.ContainsKey(x.QuizId))
                    .Sum(x => quizPointsById[x.QuizId]);
            }

            certificateScorePercent = totalQuizPoints == 0
                ? 0
                : Math.Round((earnedQuizPoints * 100.0) / totalQuizPoints, 1);

            isCertificateEligible = totalQuizCount > 0
                && completedQuizCount == totalQuizCount
                && certificateScorePercent > 80;
        }

        ViewBag.CanAccessPremium = canAccessKaliSandbox;
        ViewBag.CanAccessEntraide = canAccessKaliSandbox;
        ViewBag.CanAccessKaliSandbox = canAccessKaliSandbox;
        ViewBag.FirstCourseId = firstCourseId;
        ViewBag.TrackedCourseIds = trackedCourseIds;
        ViewBag.TrackedLessonIds = trackedLessonIds;
        ViewBag.UserTrackedLessonsCount = trackedLessonsCount;
        ViewBag.UserTotalLessonsCount = totalLessons;
        ViewBag.UserCourseProgressPercent = progressPercent;
        ViewBag.IsCertificateEligible = isCertificateEligible;
        ViewBag.EarnedQuizPoints = earnedQuizPoints;

        // Préférence de domaine
        var preferredCourseIds = new HashSet<int>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var prefClaim = await _context.UserClaims
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.ClaimType == "user_domain_preference")
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(prefClaim))
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<List<int>>(prefClaim);
                    if (ids != null) preferredCourseIds = ids.ToHashSet();
                }
                catch { }
            }
        }
        ViewBag.PreferredCourseIds = preferredCourseIds;
        ViewBag.TotalQuizPoints = totalQuizPoints;
        ViewBag.CertificateScorePercent = certificateScorePercent;
        ViewBag.CompletedQuizCount = completedQuizCount;
        ViewBag.TotalQuizCount = totalQuizCount;

        return View(courses);
    }

    private async Task<bool> HasPaidAccessAsync(string userEmail)
    {
        if (User?.IsInRole("Admin") == true)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return false;
        }

        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userEmail)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return false;
        }

        if (subscription.IsActive && subscription.Status == "active")
        {
            return true;
        }

        if (subscription.Status == "canceled")
        {
            var accessUntil = subscription.EndDate ?? subscription.StartDate.AddMonths(1);
            return DateTime.UtcNow <= accessUntil;
        }

        return false;
    }

    private static bool IsLessonEngagementsTableMissing(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            var message = current.Message;
            if (!string.IsNullOrWhiteSpace(message)
                && message.Contains("LessonEngagements", StringComparison.OrdinalIgnoreCase)
                && (message.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("doesn\u2019t exist", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
