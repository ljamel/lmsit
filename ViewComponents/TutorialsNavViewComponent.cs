using CrudDemo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrudDemo.ViewComponents
{
    public class TutorialsNavViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TutorialsNavViewComponent> _logger;

        public TutorialsNavViewComponent(ApplicationDbContext context, ILogger<TutorialsNavViewComponent> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var categories = await _context.TutorialCategories
                    .AsNoTracking()
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TutorialsNav: impossible de charger les catégories (table absente?)");
                return View(new List<CrudDemo.Models.TutorialCategory>());
            }
        }
    }
}
