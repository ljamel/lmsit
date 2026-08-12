using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using CrudDemo.Services;
using System.ServiceModel.Syndication;
using System.Xml;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using CrudDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudDemo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context)
    {
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
    }

    private const string CyberResourceUrl = "https://docs.google.com/document/d/125qy1y56yMGLpjicOo6iudWR0a42itXenYFop2bqfdo/edit?tab=t.0";

    public IActionResult Index()
    {
        return View();
    }

        public IActionResult Outils()
    {
        return View();
    }
        
    public IActionResult Orientation()
    {
        ViewBag.OrientationRole = TempData["OrientationRole"] as string;
        ViewBag.OrientationDescription = TempData["OrientationDescription"] as string;
        ViewBag.OrientationCourse = TempData["OrientationCourse"] as string;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult OutilsOrientationQuiz(int q1, int q2, int q3, int q4, int q5)
    {
        var answers = new[] { q1, q2, q3, q4, q5 };
        if (answers.Any(a => a < 1 || a > 3))
        {
            TempData["Error"] = "Veuillez répondre à toutes les questions du quiz d’orientation.";
            return RedirectToAction(nameof(Outils));
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

        if (offensiveScore >= defensiveScore && offensiveScore >= governanceScore)
        {
            role = "Pentester / Red Team";
            description = "Vous aimez explorer, tester et attaquer les systèmes pour identifier les failles avant les attaquants.";
        }
        else if (defensiveScore >= offensiveScore && defensiveScore >= governanceScore)
        {
            role = "Analyste SOC / Blue Team";
            description = "Vous avez un profil orienté surveillance, détection et réponse aux incidents.";
        }
        else
        {
            role = "GRC / Conformité Cyber";
            description = "Vous êtes orienté gestion des risques, gouvernance, politiques sécurité et conformité réglementaire.";
        }

        TempData["OrientationRole"] = role;
        TempData["OrientationDescription"] = description;

        return Redirect($"{Url.Action(nameof(Orientation), "Home")}#resultjobs");
    }

    public async Task<IActionResult> Actual()
    {
        var feeds = new List<CyberFeedItem>();

        // CERT-FR (ANSSI) – RSS officiel
        await LoadRss(
            "https://www.cert.ssi.gouv.fr/feed/",
            "CERT-FR",
            feeds
        );

        // ZATAZ – actualité cybercriminalité francophone
        await LoadRss(
            "https://www.zataz.com/feed/",
            "ZATAZ",
            feeds
        );

        // The Hacker News – actualité cyber internationale
        await LoadRss(
            "https://feeds.feedburner.com/TheHackersNews",
            "The Hacker News",
            feeds
        );

        // Decipher (pas de RSS officiel → à surveiller autrement)
        feeds.Add(new CyberFeedItem
        {
            Source = "Decipher",
            Title = "Consulter les dernières analyses cybersécurité",
            Summary = "Analyses et enquêtes approfondies sur les menaces, vulnérabilités et tendances en cybersécurité, par la rédaction de Decipher.",
            Link = "https://decipher.sc/",
            PublishedDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
        });

        // Trier par date
        feeds = feeds
            .OrderByDescending(f => f.PublishedDate)
            .ToList();

        return View(feeds);
    }

    private async Task LoadRss(string rssUrl, string sourceName, List<CyberFeedItem> feeds)
    {
        try
        {
            using var reader = XmlReader.Create(rssUrl);
            var feed = await Task.Run(() => SyndicationFeed.Load(reader));

            foreach (var item in feed.Items.Take(5))
            {
                feeds.Add(new CyberFeedItem
                {
                    Source = sourceName,
                    Title = item.Title.Text,
                    Summary = BuildSummary(item),
                    Link = item.Links.FirstOrDefault()?.Uri.ToString(),
                    PublishedDate = item.PublishDate.UtcDateTime.ToString("yyyy-MM-dd")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading RSS from {rssUrl}: {ex.Message}");
        }
    }

    private static string? BuildSummary(SyndicationItem item)
    {
        // Récupère le résumé depuis le flux RSS (Summary ou, à défaut, le premier contenu texte)
        var rawSummary = item.Summary?.Text;

        if (string.IsNullOrWhiteSpace(rawSummary) && item.Content is TextSyndicationContent textContent)
        {
            rawSummary = textContent.Text;
        }

        if (string.IsNullOrWhiteSpace(rawSummary))
        {
            return null;
        }

        // Nettoyage : suppression des balises HTML et décodage des entités
        var withoutTags = System.Text.RegularExpressions.Regex.Replace(rawSummary, "<.*?>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        var collapsedWhitespace = System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", " ").Trim();

        const int maxLength = 180;
        if (collapsedWhitespace.Length > maxLength)
        {
            collapsedWhitespace = collapsedWhitespace[..maxLength].TrimEnd() + "…";
        }

        return collapsedWhitespace;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        Response.ContentType = "application/xml; charset=utf-8";

        var tutorials = await _context.Tutorials
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .Select(t => new { t.Id, t.Slug, t.UpdatedAt })
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        ViewBag.Tutorials = tutorials;
        return View();
    }

    public IActionResult Recruting(){
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureCyberResourceEmail(string email, string? captchaAnswer)
    {
        var result = await ProcessCyberResourceLeadAsync(email, captchaAnswer);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Message;
        return Redirect(CyberResourceUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureCyberResourceEmailAjax(string email, string? captchaAnswer)
    {
        var result = await ProcessCyberResourceLeadAsync(email, captchaAnswer);

        if (!result.Success)
        {
            return Json(new
            {
                success = false,
                alreadyRegistered = result.AlreadyRegistered,
                message = result.Message
            });
        }

        return Json(new
        {
            success = true,
            message = result.Message,
            redirectUrl = CyberResourceUrl
        });
    }

    [HttpGet]
    public async Task<IActionResult> CheckCyberResourceEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Json(new { exists = false });
        }

        var normalizedEmail = email.Trim();
        var emailValidator = new EmailAddressAttribute();
        if (!emailValidator.IsValid(normalizedEmail))
        {
            return Json(new { exists = false });
        }

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        return Json(new { exists = existingUser != null });
    }

    private async Task<LeadCaptureResult> ProcessCyberResourceLeadAsync(string email, string? captchaAnswer)
    {
        if (string.IsNullOrWhiteSpace(captchaAnswer) || captchaAnswer.Trim() != "5")
        {
            return new LeadCaptureResult(false, false, "Réponse anti-bot invalide.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new LeadCaptureResult(false, false, "Veuillez saisir un email valide.");
        }

        var normalizedEmail = email.Trim();
        var emailValidator = new EmailAddressAttribute();
        if (!emailValidator.IsValid(normalizedEmail))
        {
            return new LeadCaptureResult(false, false, "Veuillez saisir un email valide.");
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return new LeadCaptureResult(false, true, "Vous êtes déjà inscrit.");
            }

            var leadUser = new IdentityUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(leadUser);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Impossible d'enregistrer le lead {Email} dans AspNetUsers. Erreurs: {Errors}",
                    normalizedEmail,
                    string.Join(" | ", createResult.Errors.Select(error => error.Description)));

                return new LeadCaptureResult(false, false, "Une erreur est survenue. Veuillez réessayer.");
            }

            var recipient = _configuration["Marketing:LeadCaptureRecipient"]
                ?? _configuration["EmailSettings:FromEmail"]
                ?? "lamri87-ingenius@yahoo.com";

            var subject = "Nouveau lead - Ressource cyber (Exit Popup)";
            var body = $@"
<p><strong>Nouvelle capture email depuis la page d'accueil.</strong></p>
<p><strong>Email:</strong> {normalizedEmail}</p>
<p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
<p><strong>Page:</strong> /Home/Index</p>";

            await _emailService.SendEmailAsync(recipient, subject, body, isHtml: true);
            return new LeadCaptureResult(true, false, "Inscription enregistrée. Vérifiez votre email pour accéder à la ressource.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la capture email pour la ressource cyber.");
            return new LeadCaptureResult(false, false, "Une erreur est survenue. Veuillez réessayer.");
        }
    }

    private sealed record LeadCaptureResult(bool Success, bool AlreadyRegistered, string Message);

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
