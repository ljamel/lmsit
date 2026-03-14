using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;
using System;
using System.Threading.Tasks;

public class CoursesListViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CoursesListViewComponent(ApplicationDbContext context)
    {
        _context = context;
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
        var canAccessKaliSandbox = await HasPaidAccessAsync(userEmail);
        ViewBag.CanAccessPremium = canAccessKaliSandbox;
        ViewBag.CanAccessEntraide = canAccessKaliSandbox;
        ViewBag.CanAccessKaliSandbox = canAccessKaliSandbox;
        ViewBag.FirstCourseId = firstCourseId;

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
}
