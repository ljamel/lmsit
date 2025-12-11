using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Data;

public class CoursesListViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CoursesListViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public IViewComponentResult Invoke()
    {
        var courses = _context.Courses
            .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
            .ToList();

        return View(courses);
    }
}
