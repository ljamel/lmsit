using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using CrudDemo.Services;
using CrudDemo.Data;
using System.Security.Cryptography;
using System.Text;

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

		public AccountController(
			UserManager<IdentityUser> userManager, 
			SignInManager<IdentityUser> signInManager, 
			RoleManager<IdentityRole> roleManager,
			IEmailService emailService,
			ILogger<AccountController> logger,
			ApplicationDbContext context)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
			_emailService = emailService;
			_logger = logger;
			_context = context;
		}

// GET: Account/Register - Redirige directement vers Stripe
	public IActionResult Register()
	{
		// Vérifier si c'est le premier utilisateur (Admin)
		var isFirstUser = !_userManager.Users.Any();

		if (isFirstUser)
		{
			// Premier utilisateur (Admin) - accès direct au formulaire
			return View("RegisterForm");
		}

		// Rediriger directement vers la création de session Stripe
		return RedirectToAction("CreatePreRegistrationSession", "Payment");
		}

		// GET: Account/RegisterForm - Formulaire accessible APRÈS paiement
		public IActionResult RegisterForm(string? sessionId)
		{
			// Vérifier si c'est le premier utilisateur (Admin)
			var isFirstUser = !_userManager.Users.Any();

			if (!isFirstUser && string.IsNullOrEmpty(sessionId))
			{
				// Pas de session de paiement - rediriger vers le paiement
				TempData["Error"] = "Vous devez d'abord effectuer le paiement.";
				return RedirectToAction("PreRegistrationCheckout", "Payment");
			}

			ViewBag.SessionId = sessionId;
			ViewBag.IsFirstUser = isFirstUser;
			return View();
		}

		// POST: Account/RegisterForm - Créer le compte après paiement
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegisterForm(RegisterViewModel model, string? sessionId)
		{
			if (ModelState.IsValid)
			{
				// Vérifier si c'est le premier utilisateur (Admin)
				var isFirstUser = !_userManager.Users.Any();

				// Si ce n'est pas le premier utilisateur, vérifier la session de paiement
				if (!isFirstUser && string.IsNullOrEmpty(sessionId))
				{
					ModelState.AddModelError("", "Session de paiement invalide. Veuillez recommencer.");
					return RedirectToAction("Register");
				}

				// Vérifier si l'utilisateur existe déjà
				var existingUser = await _userManager.FindByEmailAsync(model.Email);
				if (existingUser != null)
				{
					ModelState.AddModelError("", "Un compte existe déjà avec cet email.");
					return View(model);
				}

				var user = new IdentityUser { UserName = model.Email, Email = model.Email };
				var result = await _userManager.CreateAsync(user, model.Password);

				if (result.Succeeded)
				{
					// Créer l'abonnement si ce n'est pas le premier utilisateur
					if (!isFirstUser && !string.IsNullOrEmpty(sessionId))
					{
						var stripeSubscriptionId = TempData["StripeSubscriptionId"]?.ToString();
						var stripeCustomerId = TempData["StripeCustomerId"]?.ToString();
						
						if (!string.IsNullOrEmpty(stripeSubscriptionId))
						{
							var subscription = new Models.Subscription
							{
								UserId = user.Email!,
								StripeSubscriptionId = stripeSubscriptionId,
								StripeCustomerId = stripeCustomerId ?? "",
								Status = "active",
								IsActive = true
							};
							
							_context.Subscriptions.Add(subscription);
							await _context.SaveChangesAsync();
						}
					}

					// Envoyer un email de bienvenue
					try
					{
						await _emailService.SendRegistrationEmailAsync(user.Email!, model.Email);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Erreur lors de l'envoi de l'email de bienvenue à {Email}", user.Email);
					}

					if (isFirstUser)
					{
						const string adminRole = "Admin";
						if (!await _roleManager.RoleExistsAsync(adminRole))
						{
							await _roleManager.CreateAsync(new IdentityRole(adminRole));
						}
						await _userManager.AddToRoleAsync(user, adminRole);
					}

					await _signInManager.SignInAsync(user, isPersistent: false);
					TempData["Success"] = "Votre compte a été créé avec succès ! Bienvenue sur la plateforme.";
					return RedirectToAction("Index", "Home");
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			ViewBag.SessionId = sessionId;
			return View(model);
		}

		// Créer automatiquement le compte après paiement avec mot de passe aléatoire
		public async Task<IActionResult> CompleteRegistrationAfterPayment(string email)
		{
			// Vérifier si l'utilisateur existe déjà
			var existingUser = await _userManager.FindByEmailAsync(email);
			if (existingUser != null)
			{
				// L'utilisateur existe déjà, le connecter et rediriger vers changement de mot de passe
				await _signInManager.SignInAsync(existingUser, isPersistent: false);
				return RedirectToAction("SetPassword");
			}

			// Générer un mot de passe aléatoire sécurisé
			var randomPassword = GenerateRandomPassword(16);
			
			var user = new IdentityUser { UserName = email, Email = email };
			var result = await _userManager.CreateAsync(user, randomPassword);

			if (result.Succeeded)
			{
				// Créer l'abonnement
				var stripeSubscriptionId = TempData["StripeSubscriptionId"]?.ToString();
				var stripeCustomerId = TempData["StripeCustomerId"]?.ToString();
				
				if (!string.IsNullOrEmpty(stripeSubscriptionId))
				{
					var subscription = new Models.Subscription
					{
						UserId = user.Email!,
						StripeSubscriptionId = stripeSubscriptionId,
						StripeCustomerId = stripeCustomerId ?? "",
						Status = "active",
						IsActive = true
					};
					
					_context.Subscriptions.Add(subscription);
					await _context.SaveChangesAsync();
				}

				// Envoyer un email de bienvenue
				try
				{
					await _emailService.SendRegistrationEmailAsync(user.Email!, email);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Erreur lors de l'envoi de l'email de bienvenue à {Email}", user.Email);
				}

				// Connecter automatiquement l'utilisateur
				await _signInManager.SignInAsync(user, isPersistent: false);
				
				TempData["Success"] = "Bienvenue ! Veuillez définir votre mot de passe pour sécuriser votre compte.";
				return RedirectToAction("SetPassword");
			}

			foreach (var error in result.Errors)
			{
				TempData["Error"] = error.Description;
			}
			
			return RedirectToAction("Register");
		}

		// GET: Page pour définir le mot de passe
		[Microsoft.AspNetCore.Authorization.Authorize]
		public IActionResult SetPassword()
		{
			return View();
		}

		// POST: Définir le nouveau mot de passe
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Microsoft.AspNetCore.Authorization.Authorize]
		public async Task<IActionResult> SetPassword(string newPassword, string confirmPassword)
		{
			if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
			{
				ModelState.AddModelError("", "Le mot de passe doit contenir au moins 6 caractères.");
				return View();
			}

			if (newPassword != confirmPassword)
			{
				ModelState.AddModelError("", "Les mots de passe ne correspondent pas.");
				return View();
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return RedirectToAction("Login");
			}

			// Supprimer l'ancien mot de passe et définir le nouveau
			var removeResult = await _userManager.RemovePasswordAsync(user);
			if (removeResult.Succeeded)
			{
				var addResult = await _userManager.AddPasswordAsync(user, newPassword);
				if (addResult.Succeeded)
				{
					TempData["Success"] = "Votre mot de passe a été défini avec succès !";
					return RedirectToAction("Index", "Courses");
				}

				foreach (var error in addResult.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}
			else
			{
				foreach (var error in removeResult.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}

			return View();
		}

		// Méthode pour générer un mot de passe aléatoire sécurisé
		private string GenerateRandomPassword(int length)
		{
			const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
			var result = new StringBuilder();
			using (var rng = RandomNumberGenerator.Create())
			{
				var buffer = new byte[sizeof(uint)];
				for (int i = 0; i < length; i++)
				{
					rng.GetBytes(buffer);
					var num = BitConverter.ToUInt32(buffer, 0);
					result.Append(validChars[(int)(num % (uint)validChars.Length)]);
				}
			}
			return result.ToString();
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

		// GET: Account/ForgotPassword
		public IActionResult ForgotPassword()
		{
			return View();
		}

		// POST: Account/ForgotPassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				ModelState.AddModelError("", "Veuillez entrer votre adresse email.");
				return View();
			}

			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
			{
				// Ne pas révéler que l'utilisateur n'existe pas
				TempData["Success"] = "Si cette adresse email existe, un lien de réinitialisation a été envoyé.";
				return RedirectToAction("ForgotPasswordConfirmation");
			}

			// Générer le token de réinitialisation
			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var callbackUrl = Url.Action("ResetPassword", "Account", 
				new { token = token, email = email }, 
				protocol: Request.Scheme);

			// Envoyer l'email
			try
			{
				await _emailService.SendEmailAsync(
					email,
					"Réinitialisation de votre mot de passe",
					$"Pour réinitialiser votre mot de passe, cliquez sur ce lien : <a href='{callbackUrl}'>Réinitialiser mon mot de passe</a>"
				);

				TempData["Success"] = "Un email de réinitialisation a été envoyé à votre adresse.";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de l'envoi de l'email de réinitialisation");
				ModelState.AddModelError("", "Erreur lors de l'envoi de l'email. Veuillez réessayer.");
				return View();
			}

			return RedirectToAction("ForgotPasswordConfirmation");
		}

		// GET: Account/ForgotPasswordConfirmation
		public IActionResult ForgotPasswordConfirmation()
		{
			return View();
		}

		// GET: Account/ResetPassword
		public IActionResult ResetPassword(string token, string email)
		{
			if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
			{
				return RedirectToAction("Login");
			}

			var model = new ResetPasswordViewModel { Token = token, Email = email };
			return View(model);
		}

		// POST: Account/ResetPassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null)
			{
				// Ne pas révéler que l'utilisateur n'existe pas
				TempData["Success"] = "Votre mot de passe a été réinitialisé.";
				return RedirectToAction("Login");
			}

			var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
			if (result.Succeeded)
			{
				TempData["Success"] = "Votre mot de passe a été réinitialisé avec succès !";
				return RedirectToAction("Login");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error.Description);
			}

			return View(model);
		}	}
}