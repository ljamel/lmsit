using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CrudDemo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        var trackedCourseIds = new HashSet<int>();
        var trackedLessonIds = new HashSet<int>();
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
        ViewBag.CanAccessPremium = canAccessKaliSandbox;
        ViewBag.CanAccessEntraide = canAccessKaliSandbox;
        ViewBag.CanAccessKaliSandbox = canAccessKaliSandbox;
        ViewBag.FirstCourseId = firstCourseId;
        ViewBag.TrackedCourseIds = trackedCourseIds;
        ViewBag.TrackedLessonIds = trackedLessonIds;
        ViewBag.UserTrackedLessonsCount = trackedLessonsCount;
        ViewBag.UserTotalLessonsCount = totalLessons;
        ViewBag.UserCourseProgressPercent = progressPercent;

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
