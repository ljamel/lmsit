using CrudDemo.Data;
using CrudDemo.Models;
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

        public IActionResult Index()
        {
            return RedirectToAction("Challenges", "Courses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCtfChallenge(CreateCtfChallengeRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Formulaire invalide: vérifiez les champs du challenge.";
                return RedirectToAction(nameof(Index));
            }

            var lessonExists = await _context.Lessons
                .AsNoTracking()
                .AnyAsync(l => l.Id == request.LessonId);

            if (!lessonExists)
            {
                TempData["Error"] = "Leçon introuvable pour ce challenge.";
                return RedirectToAction(nameof(Index));
            }

            var normalizedFlag = request.Flag.Trim();
            var quiz = new Quiz
            {
                LessonId = request.LessonId,
                Question = $"[CTF] {request.Title.Trim()}",
                Description = BuildChallengePayload(request.Description, normalizedFlag),
                Points = request.Points,
                CreatedAt = DateTime.UtcNow
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Challenge CTF ajouté avec succès.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFlag(SubmitCtfFlagRequest request)
        {
            if (!await HasPaidAccessAsync())
            {
                TempData["Error"] = "Cette section nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Flag invalide.";
                return RedirectToAction(nameof(Index));
            }

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == request.QuizId);

            if (quiz == null)
            {
                TempData["Error"] = "Challenge introuvable.";
                return RedirectToAction(nameof(Index));
            }

            var (_, expectedFlag) = ExtractChallengePayload(quiz.Description);
            if (string.IsNullOrWhiteSpace(expectedFlag))
            {
                TempData["Error"] = "Ce challenge n'a pas de flag configuré.";
                return RedirectToAction(nameof(Index));
            }

            var submittedFlag = request.Flag.Trim();
            var isCorrect = string.Equals(submittedFlag, expectedFlag, StringComparison.Ordinal);
            TempData["Success"] = isCorrect
                ? "✅ Flag correct ! Challenge validé."
                : "❌ Flag incorrect. Réessaie.";

            return RedirectToAction(nameof(Index));
        }

        private static string BuildChallengePayload(string description, string flag)
        {
            var cleanDescription = description.Trim();
            return $"{cleanDescription}\n[[FLAG:{flag}]]";
        }

        private static (string PublicDescription, string? Flag) ExtractChallengePayload(string? rawDescription)
        {
            if (string.IsNullOrWhiteSpace(rawDescription))
            {
                return (string.Empty, null);
            }

            const string marker = "[[FLAG:";
            var index = rawDescription.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return (rawDescription.Trim(), null);
            }

            var end = rawDescription.IndexOf("]]", index, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
            {
                return (rawDescription.Trim(), null);
            }

            var flagStart = index + marker.Length;
            var flagLength = end - flagStart;
            var flag = flagLength > 0
                ? rawDescription.Substring(flagStart, flagLength).Trim()
                : string.Empty;

            var publicDescription = rawDescription.Remove(index, (end + 2) - index).Trim();
            return (publicDescription, string.IsNullOrWhiteSpace(flag) ? null : flag);
        }

        private static string NormalizeTitle(string question)
        {
            const string prefix = "[CTF]";
            if (question.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return question.Substring(prefix.Length).Trim();
            }

            return question;
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
