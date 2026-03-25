using CrudDemo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

public class DomainPreferenceViewComponent : ViewComponent
{
    public const string ClaimType = "user_domain_preference";

    private readonly ApplicationDbContext _context;

    public DomainPreferenceViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!(HttpContext.User.Identity?.IsAuthenticated ?? false))
            return Content(string.Empty);

        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Content(string.Empty);

        // Si la préférence existe déjà → rien à afficher
        var hasPref = await _context.UserClaims
            .AnyAsync(c => c.UserId == userId && c.ClaimType == ClaimType);

        if (hasPref)
            return Content(string.Empty);

        // Charger les cours pour les options de la modal
        var courses = await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CrudDemo.Models.Course
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description
            })
            .ToListAsync();

        return View(courses);
    }
}
