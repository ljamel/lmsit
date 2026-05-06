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
            var earnedQuizPoints = 0;
            DateTime? lastQuizAttemptAt = null;
            var totalQuizCount = await _context.Quizzes.AsNoTracking().CountAsync();
            var totalQuizPoints = await _context.Quizzes.AsNoTracking().SumAsync(q => q.Points);
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

                var quizIds = latestResultsByQuiz.Select(x => x.QuizId).Distinct().ToList();
                var quizPointsById = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => quizIds.Contains(q.Id))
                    .Select(q => new { q.Id, q.Points })
                    .ToDictionaryAsync(q => q.Id, q => q.Points);

                if (latestResultsByQuiz.Count > 0)
                {
                    quizAttempts = latestResultsByQuiz.Count;
                    quizCorrectAnswers = latestResultsByQuiz.Count(x => x.IsCorrect);
                    earnedQuizPoints = latestResultsByQuiz
                        .Where(x => x.IsCorrect && quizPointsById.ContainsKey(x.QuizId))
                        .Sum(x => quizPointsById[x.QuizId]);
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

            var successRate = totalQuizPoints == 0 ? 0 : (earnedQuizPoints * 100.0) / totalQuizPoints;
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
                EarnedQuizPoints = earnedQuizPoints,
                TotalQuizPoints = totalQuizPoints,
                QuizSuccessRate = Math.Round(successRate, 1),
                LastQuizAttemptAt = lastQuizAttemptAt,
                TotalQuizCount = totalQuizCount,
                CompletedQuizCount = completedQuizCount,
                IsCertificateEligible = isCertificateEligible
            };

            // Préférences de domaine (modules)
            var allModules = await _context.Modules
                .AsNoTracking()
                .Include(m => m.Course)
                .OrderBy(m => m.Course!.CreatedAt)
                .ThenBy(m => m.OrderIndex)
                .ToListAsync();

            var preferredModuleIds = new HashSet<int>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var prefClaim = await _context.UserClaims
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && c.ClaimType == "user_domain_preference")
                    .Select(c => c.ClaimValue)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(prefClaim))
                {
                    try
                    {
                        var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(prefClaim);
                        if (ids != null) preferredModuleIds = ids.ToHashSet();
                    }
                    catch { }
                }
            }

            ViewBag.AllModules = allModules;
            ViewBag.PreferredModuleIds = preferredModuleIds;

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

        private async Task<int?> GetFirstCourseIdAsync()
        {
            var firstCourseId = await _context.Courses
                .AsNoTracking()
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            return firstCourseId == 0 ? null : firstCourseId;
        }

        private async Task<bool> CanAccessCourseAsync(string userEmail, int courseId)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (await HasCourseAccessAsync(userEmail))
            {
                return true;
            }

            var firstCourseId = await GetFirstCourseIdAsync();
            return firstCourseId.HasValue && firstCourseId.Value == courseId;
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
            var userId = User.Identity?.Name ?? "";
            var hasPaidAccess = User.IsInRole("Admin") || await HasCourseAccessAsync(userId);

            int? allowedCourseId = null;
            if (!hasPaidAccess)
            {
                allowedCourseId = await GetFirstCourseIdAsync();
            }

            // Optimisé: AsNoTracking + projection pour ne charger que les données nécessaires
            var coursesQuery = _context.Courses
                .AsNoTracking()
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .AsSplitQuery()
                .OrderBy(c => c.OrderIndex)
                .ThenByDescending(c => c.CreatedAt);

            if (!hasPaidAccess)
            {
                if (!allowedCourseId.HasValue)
                {
                    return View(new List<Course>());
                }

                coursesQuery = coursesQuery
                    .Where(c => c.Id == allowedCourseId.Value)
                    .OrderBy(c => c.OrderIndex)
                    .ThenByDescending(c => c.CreatedAt);

                TempData["Info"] = "Accès limité: seul le premier cours est disponible sans abonnement.";
            }

            var courses = await coursesQuery.ToListAsync();

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
            var userId = User.Identity?.Name ?? "";
            var canAccess = await CanAccessCourseAsync(userId, id);
            if (!canAccess)
            {
                TempData["Error"] = "Ce cours nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
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

            var userEmail = User.Identity?.Name ?? "";
            var canAccess = await CanAccessCourseAsync(userEmail, lesson.Module!.CourseId);
            if (!canAccess)
            {
                TempData["Error"] = "Cette leçon nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

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

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TrackCourseEngagement([FromBody] TrackCourseEngagementRequest request)
        {
            return await TrackCourseEngagementInternalAsync(request.CourseId, request.LessonId);
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TrackCourseEngagementGet(int courseId, int? lessonId = null)
        {
            return await TrackCourseEngagementInternalAsync(courseId, lessonId);
        }

        private async Task<IActionResult> TrackCourseEngagementInternalAsync(int courseId, int? lessonId)
        {
            if (courseId <= 0)
            {
                return BadRequest(new { success = false, message = "CourseId invalide." });
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Unauthorized(new { success = false, message = "Utilisateur non authentifié." });
            }

            var canAccess = await CanAccessCourseAsync(userEmail, courseId);
            if (!canAccess)
            {
                return Forbid();
            }

            var now = DateTime.UtcNow;

            var enrollment = await _context.CourseEnrollments
                .Where(e => e.UserId == userEmail && e.CourseId == courseId)
                .OrderByDescending(e => e.EnrolledAt)
                .FirstOrDefaultAsync();

            if (enrollment == null)
            {
                enrollment = new CourseEnrollment
                {
                    UserId = userEmail,
                    CourseId = courseId,
                    EnrolledAt = now,
                    IsActive = true
                };

                _context.CourseEnrollments.Add(enrollment);
            }
            else
            {
                enrollment.EnrolledAt = now;
                enrollment.IsActive = true;
            }

            await _context.SaveChangesAsync();

            if (lessonId.HasValue && lessonId.Value > 0)
            {
                try
                {
                    var resolvedCourseId = await _context.Lessons
                        .AsNoTracking()
                        .Where(l => l.Id == lessonId.Value)
                        .Join(
                            _context.Modules.AsNoTracking(),
                            lesson => lesson.ModuleId,
                            module => module.Id,
                            (_, module) => module.CourseId)
                        .FirstOrDefaultAsync();

                    if (resolvedCourseId == 0 || resolvedCourseId != courseId)
                    {
                        return BadRequest(new { success = false, message = "LessonId invalide pour ce cours." });
                    }

                    var lessonEngagement = await _context.LessonEngagements
                        .Where(e => e.UserId == userEmail && e.LessonId == lessonId.Value)
                        .FirstOrDefaultAsync();

                    if (lessonEngagement == null)
                    {
                        lessonEngagement = new LessonEngagement
                        {
                            UserId = userEmail,
                            CourseId = courseId,
                            LessonId = lessonId.Value,
                            EngagedAt = now,
                            IsActive = true
                        };

                        _context.LessonEngagements.Add(lessonEngagement);
                    }
                    else
                    {
                        lessonEngagement.CourseId = courseId;
                        lessonEngagement.EngagedAt = now;
                        lessonEngagement.IsActive = true;
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex) when (IsLessonEngagementsTableMissing(ex))
                {
                    _logger.LogWarning(ex, "Table LessonEngagements absente: suivi de leçon ignoré pour User={UserEmail}, CourseId={CourseId}, LessonId={LessonId}", userEmail, courseId, lessonId);
                }
            }

            return Ok(new { success = true, courseId, lessonId, enrolledAt = now });
        }

        // Submit a quiz answer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuizAnswer(int quizId, int optionId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Lesson)
                    .ThenInclude(l => l!.Module)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            var option = await _context.QuizOptions.FindAsync(optionId);

            if (quiz == null || option == null)
                return BadRequest("Invalid quiz or option.");

            var userEmail = User.Identity?.Name ?? "";
            var canAccess = await CanAccessCourseAsync(userEmail, quiz.Lesson!.Module!.CourseId);
            if (!canAccess)
            {
                TempData["Error"] = "Ce quiz nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

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
        public async Task<IActionResult> SubmitLessonQuizAnswers(int lessonId, Dictionary<int, int> answers, Dictionary<int, string>? flagAnswers = null)
        {
            answers ??= new Dictionary<int, int>();
            flagAnswers ??= new Dictionary<int, string>();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId)
                .Include(l => l.Quizzes)
                    .ThenInclude(q => q.Options)
                .Include(l => l.Module)
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound();

            var userEmail = User.Identity?.Name ?? "";
            var canAccess = await CanAccessCourseAsync(userEmail, lesson.Module!.CourseId);
            if (!canAccess)
            {
                TempData["Error"] = "Ce quiz nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            var lessonQuizzes = lesson.Quizzes.ToList();
            var lessonQuizIds = lessonQuizzes.Select(q => q.Id).ToList();
            if (!lessonQuizIds.Any())
                return RedirectToAction(nameof(Lesson), new { id = lessonId });

            // Quizzes with options = multiple choice; without = free-text/flag
            var choiceQuizIds = lessonQuizzes.Where(q => q.Options.Any()).Select(q => q.Id).ToHashSet();
            var flagQuizIds   = lessonQuizzes.Where(q => !q.Options.Any()).Select(q => q.Id).ToHashSet();

            var totalAnswered = answers.Keys.Count(k => choiceQuizIds.Contains(k))
                              + flagAnswers.Keys.Count(k => flagQuizIds.Contains(k));

            if (totalAnswered != lessonQuizIds.Count)
            {
                TempData["Error"] = "Veuillez répondre à toutes les questions du quiz avant de valider.";
                return RedirectToAction(nameof(Lesson), new { id = lessonId });
            }

            // Validate multiple-choice answers
            var selectedOptionIds = answers.Values.Distinct().ToList();
            var selectedOptions = await _context.QuizOptions
                .AsNoTracking()
                .Where(o => selectedOptionIds.Contains(o.Id))
                .ToListAsync();
            var optionsById = selectedOptions.ToDictionary(o => o.Id);

            foreach (var quizId in choiceQuizIds)
            {
                if (!answers.TryGetValue(quizId, out var optionId)
                    || !optionsById.TryGetValue(optionId, out var option)
                    || option.QuizId != quizId)
                {
                    TempData["Error"] = "Certaines réponses sont invalides. Merci de réessayer.";
                    return RedirectToAction(nameof(Lesson), new { id = lessonId });
                }
            }

            // Prepare flag quiz map (quizId -> expected flag)
            var flagQuizMap = lessonQuizzes
                .Where(q => flagQuizIds.Contains(q.Id))
                .ToDictionary(q => q.Id, q => ExtractCtfPayload(q.Description).Item2);

            // Persist results
            var existingResults = await _context.UserQuizResults
                .Where(r => r.UserId == userId && lessonQuizIds.Contains(r.QuizId))
                .ToListAsync();
            if (existingResults.Any())
                _context.UserQuizResults.RemoveRange(existingResults);

            var now = DateTime.UtcNow;

            // Save multiple-choice results
            foreach (var quizId in choiceQuizIds)
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

            // Save flag/free-text results
            foreach (var quizId in flagQuizIds)
            {
                var submitted = flagAnswers.TryGetValue(quizId, out var raw) ? raw?.Trim() ?? "" : "";
                flagQuizMap.TryGetValue(quizId, out var expectedFlag);
                bool isCorrect = !string.IsNullOrWhiteSpace(expectedFlag)
                    ? string.Equals(submitted, expectedFlag, StringComparison.Ordinal)
                    : !string.IsNullOrWhiteSpace(submitted); // no flag configured: any non-empty answer counts

                _context.UserQuizResults.Add(new UserQuizResult
                {
                    UserId = userId,
                    QuizId = quizId,
                    SelectedOptionId = 0,
                    IsCorrect = isCorrect,
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

            var userEmail = User.Identity?.Name ?? "";
            var canAccess = await CanAccessCourseAsync(userEmail, lesson.Module!.CourseId);
            if (!canAccess)
            {
                TempData["Error"] = "Ces résultats nécessitent un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

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

            var canAccess = await CanAccessCourseAsync(userId, courseId);
            if (!canAccess)
            {
                TempData["Error"] = "Ce cours nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

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
            var userEmail = User.Identity?.Name ?? string.Empty;
            var hasPaidAccess = await HasCourseAccessAsync(userEmail);
            if (!User.IsInRole("Admin") && !hasPaidAccess)
            {
                TempData["Error"] = "Cette page nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            var latestLessons = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Module)
                    .ThenInclude(m => m!.Course)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(latestLessons);
        }

        public async Task<IActionResult> Challenges()
		{
            var userEmail = User.Identity?.Name ?? string.Empty;
            var hasPaidAccess = await HasCourseAccessAsync(userEmail);
            if (!User.IsInRole("Admin") && !hasPaidAccess)
            {
                TempData["Error"] = "Cette section nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Lesson)
                    .ThenInclude(l => l!.Module)
                        .ThenInclude(m => m!.Course)
                .OrderByDescending(q => q.CreatedAt)
                .Where(q => EF.Functions.Like(q.Question, "[CTF]%")
                    || (q.Description != null && EF.Functions.Like(q.Description, "%[[FLAG:%")))
                .ToListAsync();

            var solvedQuizIds = new HashSet<int>();
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(userId) && quizzes.Count > 0)
            {
                var challengeQuizIds = quizzes.Select(q => q.Id).ToList();
                solvedQuizIds = await _context.UserQuizResults
                    .AsNoTracking()
                    .Where(r => r.UserId == userId && r.IsCorrect && challengeQuizIds.Contains(r.QuizId))
                    .Select(r => r.QuizId)
                    .Distinct()
                    .ToHashSetAsync();
            }

            var model = new CtfChallengePageViewModel
            {
                IsAdmin = false,
                Challenges = quizzes.Select(quiz =>
                {
                    var (publicDescription, existingFlag) = ExtractCtfPayload(quiz.Description);
                    var isSolved = solvedQuizIds.Contains(quiz.Id);

                    return new CtfChallengeCardViewModel
                    {
                        QuizId = quiz.Id,
                        LessonId = quiz.LessonId,
                        Title = NormalizeCtfTitle(quiz.Question),
                        Description = publicDescription,
                        Points = quiz.Points,
                        IsSolved = isSolved,
                        CurrentFlag = isSolved ? existingFlag : null,
                        CourseTitle = quiz.Lesson?.Module?.Course?.Title ?? "-",
                        ModuleTitle = quiz.Lesson?.Module?.Title ?? "-",
                        LessonTitle = quiz.Lesson?.Title ?? "-"
                    };
                }).ToList()
            };

			return View(model);
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitChallengeFlag(SubmitCtfFlagRequest request)
        {
            var userEmail = User.Identity?.Name ?? string.Empty;
            var hasPaidAccess = await HasCourseAccessAsync(userEmail);
            if (!User.IsInRole("Admin") && !hasPaidAccess)
            {
                TempData["Error"] = "Cette section nécessite un abonnement payant.";
                return RedirectToAction("SubscriptionCheckout", "Payment");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Flag invalide.";
                return RedirectToAction(nameof(Challenges));
            }

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == request.QuizId);

            if (quiz == null)
            {
                TempData["Error"] = "Challenge introuvable.";
                return RedirectToAction(nameof(Challenges));
            }

            var (_, expectedFlag) = ExtractCtfPayload(quiz.Description);
            if (string.IsNullOrWhiteSpace(expectedFlag))
            {
                TempData["Error"] = "Ce challenge n'a pas de flag configuré.";
                return RedirectToAction(nameof(Challenges));
            }

            var submittedFlag = request.Flag.Trim();
            var isCorrect = string.Equals(submittedFlag, expectedFlag, StringComparison.Ordinal);

            if (isCorrect)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var existingResults = await _context.UserQuizResults
                        .Where(r => r.UserId == userId && r.QuizId == quiz.Id)
                        .ToListAsync();

                    if (existingResults.Any())
                    {
                        _context.UserQuizResults.RemoveRange(existingResults);
                    }

                    _context.UserQuizResults.Add(new UserQuizResult
                    {
                        UserId = userId,
                        QuizId = quiz.Id,
                        SelectedOptionId = 0,
                        IsCorrect = true,
                        AttemptedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = isCorrect
                ? "✅ Flag correct ! Challenge validé."
                : "❌ Flag incorrect. Réessaie.";

            return RedirectToAction(nameof(Challenges));
        }

        private static (string PublicDescription, string? Flag) ExtractCtfPayload(string? rawDescription)
        {
            if (string.IsNullOrWhiteSpace(rawDescription))
            {
                return (string.Empty, null);
            }

            const string marker = "[[FLAG:";
            var markerIndex = rawDescription.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return (rawDescription.Trim(), null);
            }

            var endIndex = rawDescription.IndexOf("]]", markerIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
            {
                return (rawDescription.Trim(), null);
            }

            var flagStart = markerIndex + marker.Length;
            var flagLength = endIndex - flagStart;
            var flag = flagLength > 0
                ? rawDescription.Substring(flagStart, flagLength).Trim()
                : string.Empty;

            var publicDescription = rawDescription.Remove(markerIndex, (endIndex + 2) - markerIndex).Trim();
            return (publicDescription, string.IsNullOrWhiteSpace(flag) ? null : flag);
        }

        private static string NormalizeCtfTitle(string question)
        {
            const string prefix = "[CTF]";
            if (question.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return question.Substring(prefix.Length).Trim();
            }

            return question;
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

        public sealed class TrackCourseEngagementRequest
        {
            public int CourseId { get; set; }
            public int? LessonId { get; set; }
        }

        private static bool IsLessonEngagementsTableMissing(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                var message = current.Message;
                if (!string.IsNullOrWhiteSpace(message)
                    && message.Contains("LessonEngagements", StringComparison.OrdinalIgnoreCase)
                    && (message.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase)
                        || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                        || message.Contains("doesn\u2019t exist", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

    public IActionResult Ctf()
    {
        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GAINS MENSUELS DE L'UTILISATEUR
    // Route : GET /Courses/Earnings
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Affiche les gains réels de l'utilisateur connecté pour le mois en cours
    /// ainsi que l'historique des 12 derniers mois.
    /// La donnée est persistée dans la table MonthlyEarnings.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Earnings()
    {
        var userId = _userManager.GetUserId(User);
        var userEmail = User.Identity?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Calcule, persiste et retourne les gains du mois courant
        var currentEarning = await CalculateAndPersistMonthlyEarningsAsync(userId, userEmail);

        // Historique des 12 derniers mois (mois courant inclus), du plus récent au plus ancien
        var history = await _context.MonthlyEarnings
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .Take(12)
            .ToListAsync();

        // ── Missions rémunérées ───────────────────────────────────────────────
        var now = DateTime.UtcNow;

        var activeMissions = await _context.Missions
            .AsNoTracking()
            .Where(m => m.IsActive
                        && (m.StartsAt == null || m.StartsAt <= now))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var userCompletions = await _context.UserMissionCompletions
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync();
        var completionByMissionId = userCompletions.ToDictionary(c => c.MissionId);

        var missionStatuses = activeMissions.Select(m =>
        {
            completionByMissionId.TryGetValue(m.Id, out var comp);
            return new MissionStatus
            {
                MissionId                = m.Id,
                Title                    = m.Title,
                Description              = m.Description,
                RewardAmount             = m.RewardAmount,
                RequiresAdminValidation  = m.RequiresAdminValidation,
                EndsAt                   = m.EndsAt,
                CompletionStatus         = comp?.Status,
                RewardAwarded            = comp?.RewardAwarded ?? 0m,
                CompletionId             = comp?.Id,
                ProofNote                = comp?.ProofNote,
                AdminNote                = comp?.AdminNote
            };
        }).ToList();

        var monthLabel = new System.Globalization.CultureInfo("fr-FR")
            .DateTimeFormat
            .GetMonthName(now.Month);
        monthLabel = char.ToUpper(monthLabel[0]) + monthLabel[1..];

        var viewModel = new EarningsViewModel
        {
            CurrentMonth             = currentEarning.Month,
            CurrentYear              = currentEarning.Year,
            CurrentMonthLabel        = $"{monthLabel} {currentEarning.Year}",
            LessonsCompleted         = currentEarning.LessonsCompleted,
            TotalLessonsOnPlatform   = currentEarning.TotalLessonsForMonth,
            EarnedAmount             = currentEarning.EarnedAmount,
            History                  = history,
            Missions                 = missionStatuses
        };

        return View(viewModel);
    }

    /// <summary>
    /// Permet à l'utilisateur de soumettre une mission qu'il déclare avoir accomplie.
    /// Route : POST /Courses/SubmitMission
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitMission(int missionId, string? proofNote)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var mission = await _context.Missions
            .FirstOrDefaultAsync(m => m.Id == missionId && m.IsActive);

        if (mission == null)
        {
            TempData["Error"] = "Mission introuvable ou inactive.";
            return RedirectToAction(nameof(Earnings));
        }

        // Vérifier si l'utilisateur a déjà soumis cette mission
        var existing = await _context.UserMissionCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.MissionId == missionId);

        if (existing != null)
        {
            TempData["Error"] = "Vous avez déjà soumis cette mission.";
            return RedirectToAction(nameof(Earnings));
        }

        // Vérifier le plafond de complétions si applicable
        if (mission.MaxCompletions > 0)
        {
            var approvedCount = await _context.UserMissionCompletions
                .CountAsync(c => c.MissionId == missionId && c.Status == "approved");
            if (approvedCount >= mission.MaxCompletions)
            {
                TempData["Error"] = "Cette mission a atteint son nombre maximum de complétions.";
                return RedirectToAction(nameof(Earnings));
            }
        }

        var completion = new UserMissionCompletion
        {
            UserId      = userId,
            MissionId   = missionId,
            SubmittedAt = DateTime.UtcNow,
            ProofNote   = proofNote?.Trim(),
            Status      = mission.RequiresAdminValidation ? "pending" : "approved",
            RewardAwarded = mission.RequiresAdminValidation ? 0m : mission.RewardAmount,
            ReviewedAt  = mission.RequiresAdminValidation ? null : DateTime.UtcNow
        };

        _context.UserMissionCompletions.Add(completion);
        await _context.SaveChangesAsync();

        TempData["Success"] = mission.RequiresAdminValidation
            ? "Mission soumise ! Elle sera vérifiée par un administrateur."
            : $"Mission validée automatiquement — {mission.RewardAmount:0.00} € ajoutés à vos gains.";

        return RedirectToAction(nameof(Earnings));
    }

    /// <summary>
    /// Calcule les gains réels de l'utilisateur pour le mois en cours et persiste
    /// le résultat dans la table MonthlyEarnings (INSERT ou UPDATE).
    ///
    /// Formule officielle :
    ///   gain = (leçons_terminées / total_leçons_plateforme) * 5
    ///   gain = gain / 5
    ///   gain = Math.Min(gain, 15.0)   ← plafond à 15 € par mois
    ///
    /// Les leçons terminées sont calculées de la même façon que la progression
    /// affichée dans le composant CoursesList : tous les LessonEngagements actifs
    /// (IsActive = true) de l'utilisateur, sans filtre de date.
    /// LessonEngagements.UserId stocke l'email de l'utilisateur (User.Identity.Name),
    /// identique à la logique du ViewComponent CoursesListViewComponent.
    /// </summary>
    /// <param name="userId">GUID Identity de l'utilisateur (pour MonthlyEarnings).</param>
    /// <param name="userEmail">Email de l'utilisateur (pour LessonEngagements).</param>
    private async Task<MonthlyEarning> CalculateAndPersistMonthlyEarningsAsync(string userId, string userEmail)
    {
        var now = DateTime.UtcNow;
        int currentMonth = now.Month;
        int currentYear  = now.Year;

        // ── 1. Leçons terminées par l'utilisateur ────────────────────────────
        // Même logique que CoursesListViewComponent :
        //   - UserId dans LessonEngagements = email (User.Identity.Name)
        //   - Pas de filtre sur le mois : on compte tous les engagements actifs
        //   - Distinct sur LessonId pour éviter les doublons
        var allLessonsEngaged = await _context.LessonEngagements
            .AsNoTracking()
            .Where(e => e.UserId == userEmail && e.IsActive)
            .Select(e => e.LessonId)
            .Distinct()
            .ToListAsync();

        // On ne garde que les LessonIds qui existent réellement dans la plateforme
        var allLessonIds = await _context.Lessons
            .AsNoTracking()
            .Select(l => l.Id)
            .ToListAsync();

        var allLessonIdsSet = allLessonIds.ToHashSet();
        var lessonsCompleted = allLessonsEngaged.Count(id => allLessonIdsSet.Contains(id));

        // ── 2. Total des leçons disponibles sur la plateforme ────────────────
        var totalLessonsOnPlatform = allLessonIds.Count;

        // ── 3. Application de la formule ─────────────────────────────────────
        //   gain = (leçons_terminées / total_leçons) * 5
        //   Plafond : 15 € par mois
        const double gainMultiplier = 5.0;
        const double maxMonthlyGain = 15.0;

        double rawGain = totalLessonsOnPlatform > 0
            ? (double)lessonsCompleted / totalLessonsOnPlatform * gainMultiplier
            : 0.0;

        decimal earnedAmount = (decimal)Math.Min(rawGain, maxMonthlyGain);
        earnedAmount /= 5m;
        earnedAmount = Math.Round(earnedAmount, 2); // deux décimales

        // ── 4. Persistance (INSERT ou UPDATE) ────────────────────────────────
        var existingRecord = await _context.MonthlyEarnings
            .FirstOrDefaultAsync(e => e.UserId == userId
                                      && e.Year  == currentYear
                                      && e.Month == currentMonth);

        if (existingRecord is null)
        {
            // Première visite du mois : insertion
            var newRecord = new MonthlyEarning
            {
                UserId               = userId,
                Month                = currentMonth,
                Year                 = currentYear,
                LessonsCompleted     = lessonsCompleted,
                TotalLessonsForMonth = totalLessonsOnPlatform,
                EarnedAmount         = earnedAmount,
                CalculatedAt         = DateTime.UtcNow
            };
            _context.MonthlyEarnings.Add(newRecord);
            await _context.SaveChangesAsync();
            return newRecord;
        }
        else
        {
            // Mise à jour de l'enregistrement existant
            existingRecord.LessonsCompleted     = lessonsCompleted;
            existingRecord.TotalLessonsForMonth = totalLessonsOnPlatform;
            existingRecord.EarnedAmount         = earnedAmount;
            existingRecord.CalculatedAt         = DateTime.UtcNow;

            _context.MonthlyEarnings.Update(existingRecord);
            await _context.SaveChangesAsync();
            return existingRecord;
        }
    }

    }
}
