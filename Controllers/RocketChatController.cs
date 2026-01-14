using CrudDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CrudDemo.Controllers;

public class RocketChatController : Controller
{
    private readonly IRocketChatService _rocketChatService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<RocketChatController> _logger;

    public RocketChatController(
        IRocketChatService rocketChatService, 
        UserManager<IdentityUser> userManager,
        ILogger<RocketChatController> logger)
    {
        _rocketChatService = rocketChatService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Connecter l'utilisateur authentifié à Rocket.Chat
    /// GET: /RocketChat/ConnectToChat
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ConnectToChat()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Utiliser l'email comme username si pas de UserName
            var username = !string.IsNullOrWhiteSpace(user.UserName) 
                ? user.UserName.Replace("@", "_").Replace(".", "_") // Nettoyer le username
                : user.Email?.Replace("@", "_").Replace(".", "_") ?? $"user_{user.Id}";

            var name = user.UserName ?? user.Email ?? "Utilisateur";

            _logger.LogInformation("Connexion à Rocket.Chat pour: {Username}", username);

            // Créer ou connecter l'utilisateur à Rocket.Chat
            var result = await _rocketChatService.CreateUserAsync(
                name,
                username,
                user.Email ?? $"{username}@noemail.local",
                new[] { "user" }
            );

            // Si on a un token, rediriger avec auto-connexion
            if (!string.IsNullOrEmpty(result.AuthToken))
            {
                return Redirect(result.LoginUrl);
            }
            
            // Sinon, afficher une page d'auto-connexion
            return View("AutoLogin", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion à Rocket.Chat");
            TempData["Error"] = "Impossible de se connecter au chat. Veuillez réessayer.";
            return RedirectToAction("Index", "Courses");
        }
    }

    /// <summary>
    /// Exemple: Créer un utilisateur et rediriger vers Rocket.Chat
    /// GET: /RocketChat/CreateAndRedirect?name=John&username=john123&email=john@example.com
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateAndRedirect(string name, string username, string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Les paramètres name, username et email sont requis.");
            }

            // Créer l'utilisateur et obtenir l'URL de connexion
            var loginUrl = await _rocketChatService.CreateUserAndGetLoginUrlAsync(name, username, email);

            // Rediriger vers Rocket.Chat avec connexion automatique
            return Redirect(loginUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur Rocket.Chat");
            return StatusCode(500, "Erreur lors de la création de l'utilisateur");
        }
    }

    /// <summary>
    /// Exemple: Créer un utilisateur avec des rôles personnalisés
    /// POST: /RocketChat/CreateUser
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || 
                string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Les champs name, username et email sont requis.");
            }

            var roles = request.Roles ?? new[] { "user" };
            var result = await _rocketChatService.CreateUserAsync(request.Name, request.Username, request.Email, roles);

            return Ok(new
            {
                success = true,
                userId = result.UserId,
                loginUrl = result.LoginUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur Rocket.Chat");
            return StatusCode(500, new { success = false, error = "Erreur lors de la création de l'utilisateur" });
        }
    }

    /// <summary>
    /// Page de test pour créer un utilisateur
    /// GET: /RocketChat/TestCreate
    /// </summary>
    [HttpGet]
    public IActionResult TestCreate()
    {
        return View();
    }
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[]? Roles { get; set; }
}
