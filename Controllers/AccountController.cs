using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using CrudDemo.Services;
using CrudDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudDemo.Controllers
{
	public class AccountController : Controller
	{
			private readonly UserManager<IdentityUser> _userManager;
			private readonly SignInManager<IdentityUser> _signInManager;
			private readonly RoleManager<IdentityRole> _roleManager;
			private readonly IEmailService _emailService;
			private readonly ILogger<AccountController> _logger;
			private readonly ApplicationDbContext _context;
			private readonly MattermostService _mattermostService;

			public AccountController(
				UserManager<IdentityUser> userManager, 
				SignInManager<IdentityUser> signInManager, 
				RoleManager<IdentityRole> roleManager,
				IEmailService emailService,
				ILogger<AccountController> logger,
				ApplicationDbContext context,
				MattermostService mattermostService)
			{
				_userManager = userManager;
				_signInManager = signInManager;
				_roleManager = roleManager;
				_emailService = emailService;
				_logger = logger;
				_context = context;
				_mattermostService = mattermostService;
			}

		// GET: Account/Register
		public IActionResult Register()
		{
			return View();
		}

		// POST: Account/Register
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (ModelState.IsValid)
			{
				// determine if there are any users yet (first user should become Admin)
				var isFirstUser = !_userManager.Users.Any();

				var user = new IdentityUser { UserName = model.Email, Email = model.Email };
				var result = await _userManager.CreateAsync(user, model.Password);

				if (result.Succeeded)
				{
					// Envoyer un email de bienvenue
					try
					{
						await _emailService.SendRegistrationEmailAsync(user.Email!, model.Email);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Erreur lors de l'envoi de l'email de bienvenue à {Email}", user.Email);
						// Ne pas bloquer l'inscription si l'email échoue
					}

					if (isFirstUser)
					{
						const string adminRole = "Admin";
						if (!await _roleManager.RoleExistsAsync(adminRole))
						{
							await _roleManager.CreateAsync(new IdentityRole(adminRole));
						}

						await _userManager.AddToRoleAsync(user, adminRole);
						
						// Premier utilisateur (Admin) - pas besoin de paiement
						await _signInManager.SignInAsync(user, isPersistent: false);
						return RedirectToAction("Index", "Home");
					}

					// Nouvel utilisateur normal - rediriger vers la page de paiement
					await _signInManager.SignInAsync(user, isPersistent: false);
					TempData["Message"] = "Compte créé avec succès ! Veuillez procéder au paiement pour activer votre abonnement.";
					return RedirectToAction("SubscriptionCheckout", "Payment");
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			return View(model);
		}

		// GET: Account/Login
		public IActionResult Login(string? returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		// POST: Account/Login
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;

			if (ModelState.IsValid)
			{
				var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

				if (result.Succeeded)
				{
					return LocalRedirect(returnUrl ?? "/");
				}

				if (result.IsLockedOut)
				{
					ModelState.AddModelError(string.Empty, "Account is locked.");
				}
				else
				{
					ModelState.AddModelError(string.Empty, "Invalid login attempt.");
				}
			}

			return View(model);
		}

		// GET: Account/Logout
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}

		// GET: Account/ActivateMattermost
		[Microsoft.AspNetCore.Authorization.Authorize]
		public async Task<IActionResult> ActivateMattermost()
		{
			var userEmail = User.Identity?.Name;
			if (string.IsNullOrEmpty(userEmail))
			{
				TempData["Error"] = "Utilisateur non authentifié.";
				return RedirectToAction("Index", "Courses");
			}

			// Vérifier que l'utilisateur a un abonnement actif
			var subscription = await _context.Subscriptions
				.Where(s => s.UserId == userEmail && s.IsActive && s.Status == "active")
				.FirstOrDefaultAsync();

			if (subscription == null)
			{
				TempData["Error"] = "Vous devez avoir un abonnement actif pour accéder à Mattermost.";
				return RedirectToAction("SubscriptionCheckout", "Payment");
			}

			// Vérifier si le compte Mattermost existe déjà
			if (!string.IsNullOrEmpty(subscription.MattermostUserId))
			{
				// Réactiver le compte si nécessaire
				await _mattermostService.ActivateUserAsync(subscription.MattermostUserId);
				
				// Envoyer un email de réinitialisation de mot de passe
				await _mattermostService.SendPasswordResetEmailAsync(userEmail);
				
				// Rediriger directement vers la page de reset de Mattermost
				var mattermostUrl = _mattermostService.GetMattermostUrl();
				var resetUrl = $"{mattermostUrl}/reset_password_complete?email={Uri.EscapeDataString(userEmail)}";
				return Redirect(resetUrl);
			}

			// Créer un nouveau compte Mattermost
			try
			{
				var user = await _userManager.FindByEmailAsync(userEmail);
				if (user == null)
				{
					TempData["Error"] = "Utilisateur introuvable.";
					return RedirectToAction("Index", "Courses");
				}

				// Extraire le nom d'utilisateur de l'email
				var username = userEmail.Split('@')[0].ToLower().Replace(".", "_");
				var firstName = username;
				var lastName = "";

				// Créer l'utilisateur Mattermost
				var mattermostUserId = await _mattermostService.EnsureUserAsync(
					userEmail, 
					username, 
					firstName, 
					lastName);

				if (!string.IsNullOrEmpty(mattermostUserId))
				{
					// Ajouter à l'équipe et aux canaux
					await _mattermostService.AddUserToTeamAsync(mattermostUserId);
					
					// Mettre à jour l'abonnement avec l'ID Mattermost
					subscription.MattermostUserId = mattermostUserId;
					subscription.MattermostCreatedAt = DateTime.UtcNow;
					await _context.SaveChangesAsync();

					// Envoyer un email de réinitialisation de mot de passe
					await _mattermostService.SendPasswordResetEmailAsync(userEmail);
					
					// Rediriger directement vers la page de reset de Mattermost
					var mattermostUrl = _mattermostService.GetMattermostUrl();
					var resetUrl = $"{mattermostUrl}/reset_password_complete?email={Uri.EscapeDataString(userEmail)}";
					return Redirect(resetUrl);
				}
				else
				{
					TempData["Error"] = "Erreur lors de la création du compte Mattermost.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de la création du compte Mattermost pour {Email}", userEmail);
				TempData["Error"] = "Une erreur est survenue lors de la création de votre compte Mattermost.";
			}

			return RedirectToAction("Index", "Courses");
		}
	}
}
