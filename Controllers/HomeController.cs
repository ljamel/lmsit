using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using CrudDemo.Services;
using System.ServiceModel.Syndication;
using System.Xml;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CrudDemo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        IEmailService emailService,
        IConfiguration configuration,
        UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _userManager = userManager;
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


        // Decipher (pas de RSS officiel → à surveiller autrement)
        feeds.Add(new CyberFeedItem
        {
            Source = "Decipher",
            Title = "Consulter les dernières analyses cybersécurité",
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

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Recruting(){
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureCyberResourceEmail(string email, string? captchaAnswer)
    {
        if (string.IsNullOrWhiteSpace(captchaAnswer) || captchaAnswer.Trim() != "5")
        {
            TempData["Error"] = "Réponse anti-bot invalide.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Veuillez saisir un email valide.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedEmail = email.Trim();
        var emailValidator = new EmailAddressAttribute();
        if (!emailValidator.IsValid(normalizedEmail))
        {
            TempData["Error"] = "Veuillez saisir un email valide.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser == null)
            {
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
                }
            }

            var recipient = _configuration["Marketing:LeadCaptureRecipient"]
                ?? _configuration["EmailSettings:FromEmail"]
                ?? "djamallamri@yahoo.fr";

            var subject = "Nouveau lead - Ressource cyber (Exit Popup)";
            var body = $@"
<p><strong>Nouvelle capture email depuis la page d'accueil.</strong></p>
<p><strong>Email:</strong> {normalizedEmail}</p>
<p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
<p><strong>Page:</strong> /Home/Index</p>";

            await _emailService.SendEmailAsync(recipient, subject, body, isHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la capture email pour la ressource cyber.");
        }

        return Redirect(CyberResourceUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
