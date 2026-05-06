using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMissionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminMissionsController> _logger;

        public AdminMissionsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AdminMissionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ── GET /AdminMissions ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var missions = await _context.Missions
                .AsNoTracking()
                .Include(m => m.Completions)
                .OrderByDescending(m => m.IsActive)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(missions);
        }

        // ── GET /AdminMissions/Submissions ──────────────────────────────────
        // Liste toutes les soumissions en attente de validation
        public async Task<IActionResult> Submissions()
        {
            var submissions = await _context.UserMissionCompletions
                .AsNoTracking()
                .Include(c => c.Mission)
                .OrderBy(c => c.Status == "pending" ? 0 : 1)
                .ThenByDescending(c => c.SubmittedAt)
                .ToListAsync();

            // Enrichir avec les emails des utilisateurs
            var userIds = submissions.Select(s => s.UserId).Distinct().ToList();
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();
            var userEmailById = users.ToDictionary(u => u.Id, u => u.Email ?? u.Id);

            ViewBag.UserEmailById = userEmailById;
            ViewBag.PendingCount = submissions.Count(s => s.Status == "pending");

            return View(submissions);
        }

        // ── POST /AdminMissions/Approve/{id} ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var completion = await _context.UserMissionCompletions
                .Include(c => c.Mission)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (completion == null)
            {
                TempData["Error"] = "Soumission introuvable.";
                return RedirectToAction(nameof(Submissions));
            }

            if (completion.Status != "pending")
            {
                TempData["Error"] = "Cette soumission a déjà été traitée.";
                return RedirectToAction(nameof(Submissions));
            }

            completion.Status        = "approved";
            completion.RewardAwarded = completion.Mission!.RewardAmount;
            completion.ReviewedAt    = DateTime.UtcNow;
            completion.AdminNote     = adminNote?.Trim();

            _context.UserMissionCompletions.Update(completion);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Mission {MissionId} approuvée pour l'utilisateur {UserId} — récompense {Reward}€",
                completion.MissionId, completion.UserId, completion.RewardAwarded);

            TempData["Success"] = $"Mission approuvée — {completion.RewardAwarded:0.00} € accordés à l'utilisateur.";
            return RedirectToAction(nameof(Submissions));
        }

        // ── POST /AdminMissions/Reject/{id} ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            var completion = await _context.UserMissionCompletions
                .FirstOrDefaultAsync(c => c.Id == id);

            if (completion == null)
            {
                TempData["Error"] = "Soumission introuvable.";
                return RedirectToAction(nameof(Submissions));
            }

            if (completion.Status != "pending")
            {
                TempData["Error"] = "Cette soumission a déjà été traitée.";
                return RedirectToAction(nameof(Submissions));
            }

            completion.Status     = "rejected";
            completion.ReviewedAt = DateTime.UtcNow;
            completion.AdminNote  = adminNote?.Trim();

            _context.UserMissionCompletions.Update(completion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mission refusée.";
            return RedirectToAction(nameof(Submissions));
        }

        // ── GET /AdminMissions/Create ────────────────────────────────────────
        public IActionResult Create()
        {
            return View(new Mission { Title = string.Empty, Description = string.Empty });
        }

        // ── POST /AdminMissions/Create ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,RewardAmount,MaxCompletions,IsActive,RequiresAdminValidation,StartsAt,EndsAt")]
            Mission mission)
        {
            if (!ModelState.IsValid)
                return View(mission);

            mission.CreatedBy = User.Identity?.Name ?? string.Empty;
            mission.CreatedAt = DateTime.UtcNow;
            mission.UpdatedAt = DateTime.UtcNow;

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mission « {mission.Title} » créée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /AdminMissions/Edit/{id} ─────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            return View(mission);
        }

        // ── POST /AdminMissions/Edit/{id} ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Description,RewardAmount,MaxCompletions,IsActive,RequiresAdminValidation,StartsAt,EndsAt,CreatedBy,CreatedAt")]
            Mission mission)
        {
            if (id != mission.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(mission);

            mission.UpdatedAt = DateTime.UtcNow;
            _context.Missions.Update(mission);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mission « {mission.Title} » mise à jour.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /AdminMissions/Delete/{id} ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mission « {mission.Title} » supprimée.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /AdminMissions/ToggleActive/{id} ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission == null) return NotFound();

            mission.IsActive  = !mission.IsActive;
            mission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = mission.IsActive
                ? $"Mission « {mission.Title} » activée."
                : $"Mission « {mission.Title} » désactivée.";

            return RedirectToAction(nameof(Index));
        }
    }
}
