using CrudDemo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrudDemo.Controllers
{
    [Authorize]
    public class ChallengesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChallengesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> index()
        {
            if (!await HasPaidAccessAsync())
            {
                TempData["Error"] = "Cette section nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            return View();
        }

        private async Task<bool> HasPaidAccessAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
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
}
