using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;
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
            .ToListAsync();

        return View(courses);
    }
}
