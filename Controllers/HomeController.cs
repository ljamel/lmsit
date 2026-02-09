using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using System.ServiceModel.Syndication;
using System.Xml;

namespace CrudDemo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

        public IActionResult Outils()
    {
        return View();
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
