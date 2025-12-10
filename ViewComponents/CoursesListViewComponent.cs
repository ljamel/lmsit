using Microsoft.AspNetCore.Mvc;
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
        var courses = _context.Courses.ToList();
        return View(courses);
    }
}
