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

            await SyncUserSubscriptionFromStripeAsync(userEmail);

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
            var totalQuizCount = await _context.Quizzes.AsNoTracking().CountAsync();
            var completedQuizCount = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var userQuizResults = await _context.UserQuizResults
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .Select(r => new { r.QuizId, r.IsCorrect, r.AttemptedAt })
                    .ToListAsync();

                var latestResultsByQuiz = userQuizResults
                    .GroupBy(r => r.QuizId)
                    .Select(group => group
                        .OrderByDescending(x => x.AttemptedAt)
                        .First())
                    .ToList();

                if (latestResultsByQuiz.Count > 0)
                {
                    quizAttempts = latestResultsByQuiz.Count;
                    quizCorrectAnswers = latestResultsByQuiz.Count(x => x.IsCorrect);
                    lastQuizAttemptAt = latestResultsByQuiz.Max(x => x.AttemptedAt);
                }

                completedQuizCount = latestResultsByQuiz.Count;
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
            var isCertificateEligible = totalQuizCount > 0
                && completedQuizCount == totalQuizCount
                && successRate > 80;

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
                LastQuizAttemptAt = lastQuizAttemptAt,
                TotalQuizCount = totalQuizCount,
                CompletedQuizCount = completedQuizCount,
                IsCertificateEligible = isCertificateEligible
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
            subscription.EndDate = subscription.StartDate.AddMonths(1);

            await _context.SaveChangesAsync();

            TempData["Success"] = stripeCancelFailed
                ? "Abonnement désactivé sur la plateforme, mais l'annulation Stripe a échoué. Contactez le support."
                : "Votre abonnement a été annulé avec succès.";

            return RedirectToAction(nameof(Member));
        }

        private async Task<bool> HasCourseAccessAsync(string userEmail)
        {
            await SyncUserSubscriptionFromStripeAsync(userEmail);

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

        private async Task SyncUserSubscriptionFromStripeAsync(string userEmail)
        {
            var localSubscription = await _context.Subscriptions
                .Where(s => s.UserId == userEmail)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (localSubscription == null || string.IsNullOrWhiteSpace(localSubscription.StripeSubscriptionId))
            {
                return;
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return;
            }

            try
            {
                StripeConfiguration.ApiKey = secretKey;
                var stripeService = new SubscriptionService();
                var stripeSubscription = await stripeService.GetAsync(localSubscription.StripeSubscriptionId);
                var stripeStatus = stripeSubscription?.Status ?? "inactive";
                var isStripeActive = stripeStatus == "active" || stripeStatus == "trialing";

                var changed = localSubscription.IsActive != isStripeActive
                    || localSubscription.Status != stripeStatus;

                if (!changed)
                {
                    return;
                }

                localSubscription.IsActive = isStripeActive;
                localSubscription.Status = stripeStatus;

                if (isStripeActive)
                {
                    localSubscription.EndDate = null;
                    localSubscription.CanceledAt = null;
                }
                else
                {
                    localSubscription.EndDate ??= DateTime.UtcNow;
                    if (stripeStatus == "canceled")
                    {
                        localSubscription.CanceledAt ??= DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync Stripe impossible pour {UserEmail}", userEmail);
            }
        }

        // List all courses (public view for authenticated users)
        public async Task<IActionResult> Index()
        {
            // Vérifier si l'utilisateur a un abonnement actif (sauf Admin)
            if (!User.IsInRole("Admin") && !User.IsInRole("Free"))
            {
                var userId = User.Identity?.Name ?? "";
                var hasActiveSubscription = await HasCourseAccessAsync(userId);

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

        [HttpPost]
        public async Task<IActionResult> OrientationQuizV2(int q1, int q2, int q3, int q4, int q5, string? returnUrl)
        {
            var fallbackUrl = Url.Action(nameof(Index), "Courses") ?? "/Courses";
            var refererValue = Request.Headers.Referer.ToString();
            var refererPath = Uri.TryCreate(refererValue, UriKind.Absolute, out var refererUri)
                ? refererUri.PathAndQuery
                : null;

            var targetUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : (!string.IsNullOrWhiteSpace(refererPath) && Url.IsLocalUrl(refererPath)
                    ? refererPath
                    : fallbackUrl);

            string BuildAnchorUrl(string url, string anchor)
            {
                var baseUrl = url.Split('#')[0];
                return $"{baseUrl}#{anchor}";
            }

            var answers = new[] { q1, q2, q3, q4, q5 };
            if (answers.Any(a => a < 1 || a > 3))
            {
                TempData["Error"] = "Veuillez répondre à toutes les questions du quiz d’orientation.";
                return LocalRedirect(BuildAnchorUrl(targetUrl, "orientation1"));
            }

            var offensiveScore = answers.Count(a => a == 1);
            var defensiveScore = answers.Count(a => a == 2);
            var governanceScore = answers.Count(a => a == 3);

            string role;
            string description;
            string course;

            if (offensiveScore >= defensiveScore && offensiveScore >= governanceScore)
            {
                role = "Pentester / Attaquant";
                description = "Vous avez un profil curieux et explorateur : vous aimez comprendre les failles et tester la sécurité des systèmes.";
                course = "Parcours conseillé: Introduction au Hacking → Bien débuter (outils, SSH, Hydra, wordlists) → Reconnaissance (Nmap, NSE, Curl CLI) → Hacking de données (SQLi, SQLMap, Google dork) → Metasploit recon→exploit → Réseaux/Wifi (Wireshark, Bettercap, Aircrack-ng) → Challenges (CTF/HTB).";
            }
            else if (defensiveScore >= offensiveScore && defensiveScore >= governanceScore)
            {
                role = "Analyste SOC / Défenseur";
                description = "Vous avez un profil défense et réaction : vous gardez votre calme, détectez les attaques et rétablissez rapidement les services.";
                course = "Parcours conseillé: Introduction à la Cybersécurité → Analyst SOC (analyse des logs, qualification d’événements) → Monitoring SIEM/IDS/HIDS (Suricata, Wazuh, Splunk) → Réseaux (Wireshark, détection MITM) → Réponse à incident et amélioration continue.";
            }
            else
            {
                role = "GRC / Droit informatique";
                description = "Vous avez un profil organisation et stratégie : vous aimez structurer, prévenir les risques et améliorer durablement la sécurité.";
                course = "Parcours conseillé: Introduction à la Cybersécurité (bases + métiers) → Réglementations & standards (ISO 27001/27002/27005, RGPD, NIST, PCI-DSS) → Gestion des risques → Gouvernance sécurité → Pilotage de plans d’action et contrôles.";
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

            return LocalRedirect(BuildAnchorUrl(targetUrl, "resultjobs"));
        }

        [HttpGet]
        public IActionResult OrientationQuizV2()
        {
            var fallbackUrl = Url.Action(nameof(Index), "Courses") ?? "/Courses";
            var refererValue = Request.Headers.Referer.ToString();
            var refererPath = Uri.TryCreate(refererValue, UriKind.Absolute, out var refererUri)
                ? refererUri.PathAndQuery
                : null;

            if (!string.IsNullOrWhiteSpace(refererPath) && Url.IsLocalUrl(refererPath))
            {
                return LocalRedirect(refererPath);
            }

            return LocalRedirect(fallbackUrl);
        }

        [HttpGet]
        public IActionResult OrientationQuiz()
        {
            return RedirectToAction(nameof(Index));
        }


        // View course details with modules and lessons
        public async Task<IActionResult> Details(int id)
        {
            // Vérifier si l'utilisateur a un abonnement actif (sauf Admin ou Free)
            if (!User.IsInRole("Admin") && !User.IsInRole("Free"))
            {
                var userId = User.Identity?.Name ?? "";
                var hasActiveSubscription = await HasCourseAccessAsync(userId);

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
                var hasActiveSubscription = await HasCourseAccessAsync(userEmail);

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
                .AsSplitQuery()
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
            ViewBag.OrientationRole = TempData["OrientationRole"] as string;
            ViewBag.OrientationDescription = TempData["OrientationDescription"] as string;
            ViewBag.OrientationCourse = TempData["OrientationCourse"] as string;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLessonQuizAnswers(int lessonId, Dictionary<int, int> answers)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId)
                .Include(l => l.Quizzes)
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound();

            var lessonQuizIds = lesson.Quizzes.Select(q => q.Id).ToList();
            if (!lessonQuizIds.Any())
                return RedirectToAction(nameof(Lesson), new { id = lessonId });

            if (answers.Count != lessonQuizIds.Count)
            {
                TempData["Error"] = "Veuillez répondre à toutes les questions du quiz avant de valider.";
                return RedirectToAction(nameof(Lesson), new { id = lessonId });
            }

            var selectedOptionIds = answers.Values.Distinct().ToList();
            var selectedOptions = await _context.QuizOptions
                .AsNoTracking()
                .Where(o => selectedOptionIds.Contains(o.Id))
                .ToListAsync();

            var optionsById = selectedOptions.ToDictionary(o => o.Id);

            foreach (var quizId in lessonQuizIds)
            {
                if (!answers.TryGetValue(quizId, out var optionId)
                    || !optionsById.TryGetValue(optionId, out var option)
                    || option.QuizId != quizId)
                {
                    TempData["Error"] = "Certaines réponses sont invalides. Merci de réessayer.";
                    return RedirectToAction(nameof(Lesson), new { id = lessonId });
                }
            }

            var existingResults = await _context.UserQuizResults
                .Where(r => r.UserId == userId && lessonQuizIds.Contains(r.QuizId))
                .ToListAsync();

            if (existingResults.Any())
            {
                _context.UserQuizResults.RemoveRange(existingResults);
            }

            var now = DateTime.UtcNow;
            foreach (var quizId in lessonQuizIds)
            {
                var optionId = answers[quizId];
                var option = optionsById[optionId];

                _context.UserQuizResults.Add(new UserQuizResult
                {
                    UserId = userId,
                    QuizId = quizId,
                    SelectedOptionId = optionId,
                    IsCorrect = option.IsCorrect,
                    AttemptedAt = now
                });
            }

            await _context.SaveChangesAsync();

            return LocalRedirect($"/QuizResults?lessonId={lessonId}");
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

        public IActionResult Challenges()
		{
            if (User.IsInRole("Free"))
            {
                TempData["Error"] = "Cette section n'est pas accessible avec le plan Free.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

			return View();
		}

        public async Task<IActionResult> Certif1()
		{
            if (User.IsInRole("Free"))
            {
                TempData["Error"] = "Cette section n'est pas accessible avec le plan Free.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            var learnerFullName = string.Empty;
            var learnerFirstName = string.Empty;
            var learnerLastName = string.Empty;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var subscription = await _context.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userEmail)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

                if (subscription != null && !string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
                {
                    try
                    {
                        var secretKey = _configuration["Stripe:SecretKey"];
                        if (!string.IsNullOrWhiteSpace(secretKey))
                        {
                            StripeConfiguration.ApiKey = secretKey;
                            var customerService = new CustomerService();
                            var customer = await customerService.GetAsync(subscription.StripeCustomerId);
                            var stripeName = customer?.Name?.Trim() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(stripeName))
                            {
                                learnerFullName = stripeName;
                                var firstSeparator = stripeName.IndexOf(' ');
                                if (firstSeparator > 0)
                                {
                                    learnerFirstName = stripeName.Substring(0, firstSeparator).Trim();
                                    learnerLastName = stripeName.Substring(firstSeparator + 1).Trim();
                                }
                                else
                                {
                                    learnerFirstName = stripeName;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de récupérer le nom Stripe pour l'utilisateur {UserEmail}", userEmail);
                    }
                }
            }

            ViewBag.LearnerFullName = learnerFullName;
            ViewBag.LearnerFirstName = learnerFirstName;
            ViewBag.LearnerLastName = learnerLastName;

			return View();
		}

    }
}
