using CrudDemo.Data;
using CrudDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            ILogger<CoursesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Member()
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User);

            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userEmail)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            var currentSubscriptionPriceEur = 19m;

            var quizAttempts = 0;
            var quizCorrectAnswers = 0;
            DateTime? lastQuizAttemptAt = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var quizStats = await _context.UserQuizResults
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .GroupBy(_ => 1)
                    .Select(group => new
                    {
                        Attempts = group.Count(),
                        CorrectAnswers = group.Count(x => x.IsCorrect),
                        LastAttemptAt = group.Max(x => x.AttemptedAt)
                    })
                    .FirstOrDefaultAsync();

                if (quizStats != null)
                {
                    quizAttempts = quizStats.Attempts;
                    quizCorrectAnswers = quizStats.CorrectAnswers;
                    lastQuizAttemptAt = quizStats.LastAttemptAt;
                }
            }

            string? orientationRole = null;
            string? orientationDescription = null;
            string? orientationCourse = null;

            if (!string.IsNullOrEmpty(userId))
            {
                const string orientationClaimType = "cyber_orientation_result";
                var orientationClaim = await _context.UserClaims
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && c.ClaimType == orientationClaimType)
                    .OrderByDescending(c => c.Id)
                    .Select(c => c.ClaimValue)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(orientationClaim))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(orientationClaim);
                        var root = document.RootElement;

                        if (root.TryGetProperty("role", out var roleElement))
                        {
                            orientationRole = roleElement.GetString();
                        }

                        if (root.TryGetProperty("description", out var descriptionElement))
                        {
                            orientationDescription = descriptionElement.GetString();
                        }

                        if (root.TryGetProperty("course", out var courseElement))
                        {
                            orientationCourse = courseElement.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }
            }

            var successRate = quizAttempts == 0 ? 0 : (quizCorrectAnswers * 100.0) / quizAttempts;

            var memberProfile = new MemberProfileViewModel
            {
                Subscription = subscription,
                Email = userEmail,
                CurrentSubscriptionPriceEur = currentSubscriptionPriceEur,
                OrientationRole = orientationRole,
                OrientationDescription = orientationDescription,
                OrientationCourse = orientationCourse,
                QuizAttempts = quizAttempts,
                QuizCorrectAnswers = quizCorrectAnswers,
                QuizSuccessRate = Math.Round(successRate, 1),
                LastQuizAttemptAt = lastQuizAttemptAt
            };

            return View(memberProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription()
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userEmail && s.IsActive)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                TempData["Error"] = "Aucun abonnement actif trouvé.";
                return RedirectToAction(nameof(Member));
            }

            var stripeCancelFailed = false;

            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
            {
                try
                {
                    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
                    var stripeService = new SubscriptionService();
                    await stripeService.CancelAsync(subscription.StripeSubscriptionId);
                }
                catch (Exception ex)
                {
                    stripeCancelFailed = true;
                    _logger.LogWarning(ex, "Erreur lors de l'annulation Stripe pour l'utilisateur {UserEmail}", userEmail);
                }
            }

            subscription.IsActive = false;
            subscription.Status = "canceled";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = stripeCancelFailed
                ? "Abonnement désactivé sur la plateforme, mais l'annulation Stripe a échoué. Contactez le support."
                : "Votre abonnement a été annulé avec succès.";

            return RedirectToAction(nameof(Member));
        }

        // List all courses (public view for authenticated users)
        public async Task<IActionResult> Index()
        {
            // Vérifier si l'utilisateur a un abonnement actif (sauf Admin)
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity?.Name ?? "";
                var hasActiveSubscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive && s.Status == "active")
                    .AnyAsync();

                if (!hasActiveSubscription)
                {
                    TempData["Error"] = "Vous devez avoir un abonnement actif pour accéder aux cours.";
                    return RedirectToAction("SubscriptionCheckout", "Payment");
                }
            }

            // Optimisé: AsNoTracking + projection pour ne charger que les données nécessaires
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .AsSplitQuery()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Charger les commentaires pour tous les cours
            var courseIds = courses.Select(c => c.Id).ToList();
            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => courseIds.Contains(c.CourseId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comments = comments;
            ViewBag.OrientationRole = TempData["OrientationRole"] as string;
            ViewBag.OrientationDescription = TempData["OrientationDescription"] as string;
            ViewBag.OrientationCourse = TempData["OrientationCourse"] as string;

            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrientationQuiz(int q1, int q2, int q3, int q4, int q5)
        {
            var answers = new[] { q1, q2, q3, q4, q5 };
            if (answers.Any(a => a < 1 || a > 3))
            {
                TempData["Error"] = "Veuillez répondre à toutes les questions du quiz d’orientation.";
                return RedirectToAction(nameof(Index));
            }

            var offensiveScore = 0;
            var defensiveScore = 0;
            var governanceScore = 0;

            void AddScores(int answer, int offensiveWeight, int defensiveWeight, int governanceWeight)
            {
                if (answer == 1) offensiveScore += offensiveWeight;
                if (answer == 2) defensiveScore += defensiveWeight;
                if (answer == 3) governanceScore += governanceWeight;
            }

            AddScores(q1, 2, 2, 2);
            AddScores(q2, 3, 3, 3);
            AddScores(q3, 2, 3, 3);
            AddScores(q4, 3, 3, 3);
            AddScores(q5, 2, 2, 3);

            string role;
            string description;
            string course;

            if (offensiveScore >= defensiveScore && offensiveScore >= governanceScore)
            {
                role = "Pentester / Red Team";
                description = "Vous aimez explorer, tester et attaquer les systèmes pour identifier les failles avant les attaquants.";
                course = "Parcours conseillé: Introduction à la Cybersécurité (Introduction au hacking, Comprendre les failles, Les bases) → Bien débuter (outils, SSH, Hydra, wordlists) → Phase de reconnaissance (Nmap avancé, NSE, Curl CLI) → Hacking de données (SQL Injection 101, SQLMap, Google dork) → Metasploit recon→exploit (WordPress XML-RPC, fichiers .rc) → Réseaux/Wifi (Wireshark, Bettercap MITM, Aircrack-ng) → Programmation (XSS, Python pour le hacking) → BLACKHAT offensive (offensive security, rootkit) → Challenges (CTF/HTB).";
            }
            else if (defensiveScore >= offensiveScore && defensiveScore >= governanceScore)
            {
                role = "Analyste SOC / Blue Team";
                description = "Vous avez un profil orienté surveillance, détection et réponse aux incidents.";
                course = "Parcours conseillé: Introduction à la Cybersécurité (bases, termes techniques, métiers cyber) → Cryptanalyse (chiffrement, encodage, hashage) → Analyst SOC (analyse des LOGs, qualification d’événements) → Monitoring SIEM/IDS/HIDS (Suricata, Wazuh agent, Splunk + Syslog Linux) → Réseaux (Wireshark, détection MITM Bettercap) → Windows serveur (installation, AD, lab) → IA et cyber (Gemini CLI, usages IA en SOC).";
            }
            else
            {
                role = "GRC / Conformité Cyber";
                description = "Vous êtes orienté gestion des risques, gouvernance, politiques sécurité et conformité réglementaire.";
                course = "Parcours conseillé: Introduction à la Cybersécurité (bases + métiers) → Réglementations & Standards » (ISO 27001 27002 27005, RGPD, NIST, PCI-DSS…) → Monitoring SIEM/IDS/HIDS (vision gouvernance opérationnelle: Suricata/Wazuh/Splunk) → Windows serveur (AD, organisation des accès)";
            }

            TempData["OrientationRole"] = role;
            TempData["OrientationDescription"] = description;
            TempData["OrientationCourse"] = course;

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                const string orientationClaimType = "cyber_orientation_result";
                var claimValue = JsonSerializer.Serialize(new
                {
                    role,
                    description,
                    course,
                    updatedAtUtc = DateTime.UtcNow
                });

                var existingClaim = await _context.UserClaims
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == orientationClaimType);

                if (existingClaim == null)
                {
                    _context.UserClaims.Add(new IdentityUserClaim<string>
                    {
                        UserId = userId,
                        ClaimType = orientationClaimType,
                        ClaimValue = claimValue
                    });
                }
                else
                {
                    existingClaim.ClaimValue = claimValue;
                }

                await _context.SaveChangesAsync();
            }

            return Redirect($"{Url.Action(nameof(Index), "Courses")}#resultjobs");
        }

        [HttpGet]
        public IActionResult OrientationQuiz()
        {
            return RedirectToAction(nameof(Index));
        }


        // View course details with modules and lessons
        public async Task<IActionResult> Details(int id)
        {
            // Vérifier si l'utilisateur a un abonnement actif (sauf Admin)
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity?.Name ?? "";
                var hasActiveSubscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive && s.Status == "active")
                    .AnyAsync();

                if (!hasActiveSubscription)
                {
                    TempData["Error"] = "Vous devez avoir un abonnement actif pour accéder à ce cours.";
                    return RedirectToAction("SubscriptionCheckout", "Payment");
                }
            }

            // Optimisé: AsNoTracking + filtre WHERE précoce
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                    .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound();
            
            // Vérifier si l'utilisateur est déjà inscrit
            var userEmail = User.Identity?.Name ?? "";
            var enrollment = await _context.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.UserId == userEmail && e.CourseId == id && e.IsActive)
                .FirstOrDefaultAsync();
            
            ViewBag.IsEnrolled = enrollment != null;

            // Charger les commentaires pour ce cours
            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.CourseId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comments = comments;
            ViewBag.CourseId = id;

            return View(course);
        }

        // View a specific lesson and video
        public async Task<IActionResult> Lesson(int id, string? lessonSearch)
        {
            // Vérifier si l'utilisateur a un abonnement actif (sauf Admin)
            if (!User.IsInRole("Admin"))
            {
                var userEmail = User.Identity?.Name ?? "";
                var hasActiveSubscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userEmail && s.IsActive && s.Status == "active")
                    .AnyAsync();

                if (!hasActiveSubscription)
                {
                    TempData["Error"] = "Vous devez avoir un abonnement actif pour accéder aux leçons.";
                    return RedirectToAction("SubscriptionCheckout", "Payment");
                }
            }

            // Optimisé: Une seule requête avec Include pour charger tout d'un coup (évite N+1)
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == id)
                .Include(l => l.Module)
                    .ThenInclude(m => m!.Course)
                .Include(l => l.Quizzes)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound();

            // Get the current user's previous quiz attempts for this lesson
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId) && lesson.Quizzes?.Any() == true)
            {
                var quizIds = lesson.Quizzes.Select(q => q.Id).ToList();
                var userAttempts = await _context.UserQuizResults
                    .AsNoTracking()
                    .Where(r => r.UserId == userId && quizIds.Contains(r.QuizId))
                    .ToListAsync();

                ViewBag.UserAttempts = userAttempts;
            }

            // Charger les commentaires pour ce cours
            var courseId = lesson.Module!.CourseId;
            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.CourseId == courseId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comments = comments;
            ViewBag.CourseId = courseId;

            if (!string.IsNullOrWhiteSpace(lessonSearch))
            {
                var term = lessonSearch.Trim();
                var likePattern = $"%{term}%";

                var relatedLessons = await _context.Lessons
                    .AsNoTracking()
                    .Include(l => l.Module)
                    .Where(l => l.Module != null && l.Module.CourseId == courseId)
                    .Where(l => l.Description != null && EF.Functions.Like(l.Description, likePattern))
                    .OrderBy(l => l.Module!.OrderIndex)
                    .ThenBy(l => l.OrderIndex)
                    .ToListAsync();

                ViewBag.LessonSearchTerm = term;
                ViewBag.LessonSearchResults = relatedLessons;
            }

            return View(lesson);
        }

        // Submit a quiz answer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuizAnswer(int quizId, int optionId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            var option = await _context.QuizOptions.FindAsync(optionId);

            if (quiz == null || option == null)
                return BadRequest("Invalid quiz or option.");

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            // Supprimer les anciennes réponses pour ce quiz (permettre de réessayer)
            var existingResults = await _context.UserQuizResults
                .Where(r => r.UserId == userId && r.QuizId == quizId)
                .ToListAsync();
            
            if (existingResults.Any())
            {
                _context.UserQuizResults.RemoveRange(existingResults);
            }

            var result = new UserQuizResult
            {
                UserId = userId,
                QuizId = quizId,
                SelectedOptionId = optionId,
                IsCorrect = option.IsCorrect,
                AttemptedAt = DateTime.UtcNow
            };

            _context.UserQuizResults.Add(result);
            await _context.SaveChangesAsync();

            // Redirect back to lesson with a success message
            var lesson = await _context.Lessons.FindAsync(quiz.LessonId);
            if (lesson == null)
                return NotFound();

            return LocalRedirect($"/QuizResults?lessonId={lesson.Id}");
        }

        // Show quiz results for a lesson
        [HttpGet("/QuizResults")]
        public async Task<IActionResult> QuizResults(int lessonId)
        {
            // Optimisé: Charger tout en une requête
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId)
                .Include(l => l.Module)
                    .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            // Optimisé: Une seule requête avec projection
            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.LessonId == lessonId)
                .ToListAsync();

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var results = await _context.UserQuizResults
                .AsNoTracking()
                .Where(r => r.UserId == userId && quizIds.Contains(r.QuizId))
                .Include(r => r.Quiz)
                    .ThenInclude(q => q!.Options)
                .Include(r => r.SelectedOption)
                .ToListAsync();

            ViewBag.Lesson = lesson;
            ViewBag.CorrectCount = results.Count(r => r.IsCorrect);
            ViewBag.TotalQuestions = quizzes.Count;

            return View(results);
        }

        // Add comment to a course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int courseId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            {
                TempData["Error"] = "Le commentaire doit contenir entre 1 et 1000 caractères.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var comment = new Comment
            {
                CourseId = courseId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Commentaire ajouté avec succès!";
            return RedirectToAction(nameof(Index));
        }

        // Delete a comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound();

            var userId = User.Identity?.Name ?? "";
            
            // Only the comment author or admin can delete
            if (comment.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Commentaire supprimé avec succès!";
            return RedirectToAction(nameof(Index));
        }

            public async Task<IActionResult> Event()
            {
                var latestLessons = await _context.Lessons
                    .AsNoTracking()
                    .Include(l => l.Module)
                        .ThenInclude(m => m!.Course)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return View(latestLessons);
            }
    }
}
