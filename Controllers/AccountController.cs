using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;
using CrudDemo.Services;
using CrudDemo.Data;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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

		// GET: Account/Register
		public IActionResult Register()
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				return RedirectToAction("Index", "Courses");
			}

			var isFirstUser = !_userManager.Users.Any();

			if (isFirstUser)
			{
				return View("RegisterForm");
			}

			return RedirectToAction("CreatePreRegistrationSession", "Payment");
		}

		// GET: Account/RegisterForm
		public IActionResult RegisterForm(string? sessionId)
		{
			var isFirstUser = !_userManager.Users.Any();

			if (!isFirstUser && string.IsNullOrEmpty(sessionId))
			{
				TempData["Error"] = "Vous devez d'abord valider votre accès d'essai de 3 jours pour 1 euro.";
				return RedirectToAction("PreRegistrationCheckout", "Payment");
			}

			ViewBag.SessionId = sessionId;
			ViewBag.IsFirstUser = isFirstUser;
			return View();
		}

		// POST: Account/RegisterForm
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegisterForm(RegisterViewModel model, string? sessionId)
		{
			if (ModelState.IsValid)
			{
				var isFirstUser = !_userManager.Users.Any();

				if (!isFirstUser && string.IsNullOrEmpty(sessionId))
				{
					ModelState.AddModelError("", "Session d'abonnement d'essai invalide. Veuillez recommencer.");
					return RedirectToAction("Register");
				}

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
					if (!isFirstUser)
					{
						var stripeSubscriptionId = TempData["StripeSubscriptionId"]?.ToString();
						var stripeCustomerId = TempData["StripeCustomerId"]?.ToString();

						string? effectiveSessionId = sessionId;
						if (stripeSubscriptionId != null && stripeSubscriptionId.StartsWith("cs_"))
						{
							effectiveSessionId = stripeSubscriptionId;
							stripeSubscriptionId = null;
						}

						if (!string.IsNullOrEmpty(effectiveSessionId))
						{
							try
							{
								var sessionService = new Stripe.Checkout.SessionService();
								var stripeSession = await sessionService.GetAsync(effectiveSessionId);

								stripeCustomerId = stripeSession.CustomerId;
								stripeSubscriptionId = stripeSession.SubscriptionId;
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Erreur lors de la récupération de la session {SessionId} dans RegisterForm", effectiveSessionId);
							}
						}
						else if (!string.IsNullOrEmpty(stripeSubscriptionId) && string.IsNullOrEmpty(stripeCustomerId))
						{
							try
							{
								var subscriptionService = new Stripe.SubscriptionService();
								var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);
								stripeCustomerId = stripeSubscription.CustomerId;
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Erreur lors de la récupération de l'abonnement {SubscriptionId} dans RegisterForm", stripeSubscriptionId);
							}
						}

						if (!string.IsNullOrEmpty(stripeSubscriptionId) && stripeSubscriptionId.StartsWith("sub_"))
						{
							var subscription = new Models.Subscription
							{
								UserId = user.Email!,
								StripeSubscriptionId = stripeSubscriptionId,
								StripeCustomerId = stripeCustomerId ?? "",
								Status = "trialing", // Conserve le statut trialing car la période d'essai est active
								IsActive = true
							};

							_context.Subscriptions.Add(subscription);
							await _context.SaveChangesAsync();
						}
						else
						{
							_logger.LogCritical("Abonnement d'essai introuvable pour {Email} (Valeur lue : {SubId}).", user.Email, stripeSubscriptionId);
							TempData["Warning"] = "Compte créé, mais la liaison avec vos 3 jours d'essai a échoué. Contactez le support.";
						}
					}

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
					TempData["Success"] = "Votre compte a été configuré avec succès et vos 3 jours d'essai sont ouverts !";
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

		// CompleteRegistrationAfterPayment (Inscription automatique après paiement)
		public async Task<IActionResult> CompleteRegistrationAfterPayment(string email)
		{
			var existingUser = await _userManager.FindByEmailAsync(email);
			if (existingUser != null)
			{
				await _signInManager.SignInAsync(existingUser, isPersistent: false);
				return RedirectToAction("SetPassword");
			}

			var randomPassword = GenerateRandomPassword(16);

			var user = new IdentityUser { UserName = email, Email = email };
			var result = await _userManager.CreateAsync(user, randomPassword);

			if (result.Succeeded)
			{
				var stripeSubscriptionId = TempData["StripeSubscriptionId"]?.ToString();
				var stripeCustomerId = TempData["StripeCustomerId"]?.ToString();

				string? effectiveSessionId = null;
				if (stripeSubscriptionId != null && stripeSubscriptionId.StartsWith("cs_"))
				{
					effectiveSessionId = stripeSubscriptionId;
					stripeSubscriptionId = null;
				}

				if (!string.IsNullOrEmpty(effectiveSessionId))
				{
					try
					{
						var sessionService = new Stripe.Checkout.SessionService();
						var stripeSession = await sessionService.GetAsync(effectiveSessionId);

						stripeCustomerId = stripeSession.CustomerId;
						stripeSubscriptionId = stripeSession.SubscriptionId;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Erreur lors du traitement Stripe dans CompleteRegistrationAfterPayment pour la session {SessionId}", effectiveSessionId);
					}
				}
				else if (!string.IsNullOrEmpty(stripeSubscriptionId) && string.IsNullOrEmpty(stripeCustomerId))
				{
					try
					{
						var subscriptionService = new Stripe.SubscriptionService();
						var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);
						stripeCustomerId = stripeSubscription.CustomerId;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Erreur lors de la récupération de l'abonnement Stripe {SubscriptionId} dans CompleteRegistrationAfterPayment", stripeSubscriptionId);
					}
				}

				if (!string.IsNullOrEmpty(stripeSubscriptionId) && stripeSubscriptionId.StartsWith("sub_"))
				{
					var subscription = new Models.Subscription
					{
						UserId = user.Email!,
						StripeSubscriptionId = stripeSubscriptionId,
						StripeCustomerId = stripeCustomerId ?? "",
						Status = "trialing",
						IsActive = true
					};

					_context.Subscriptions.Add(subscription);
					await _context.SaveChangesAsync();
				}
				else
				{
					_logger.LogCritical("Abonnement d'essai annuel introuvable lors de la création automatique pour {Email}.", email);
				}

				try
				{
					await _emailService.SendRegistrationEmailAsync(user.Email!, email);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Erreur lors de l'envoi de l'email de bienvenue à {Email}", user.Email);
				}

				await _signInManager.SignInAsync(user, isPersistent: false);

				TempData["Success"] = "Bienvenue ! Votre essai de 3 jours a été validé par votre paiement de 1 euro. Définissez votre mot de passe.";
				return RedirectToAction("SetPassword");
			}

			foreach (var error in result.Errors)
			{
				TempData["Error"] = error.Description;
			}

			return RedirectToAction("Register");
		}

		// GET: Account/SetPassword
		[Microsoft.AspNetCore.Authorization.Authorize]
		public IActionResult SetPassword()
		{
			return View();
		}

		// POST: Account/SetPassword
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
				var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: true);

				if (result.Succeeded)
				{
					return RedirectToAction("Index", "Courses");
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
				TempData["Success"] = "Si cette adresse email existe, un lien de réinitialisation a été envoyé.";
				return RedirectToAction("ForgotPasswordConfirmation");
			}

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var callbackUrl = Url.Action("ResetPassword", "Account",
				new { token = token, email = email },
				protocol: Request.Scheme);

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
		}

		// POST: Account/SaveDomainPreference
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveDomainPreference(List<int>? moduleIds)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized();

			const string claimType = "user_domain_preference";

			var existing = await _userManager.GetClaimsAsync(user);
			foreach (var c in existing.Where(c => c.Type == claimType))
				await _userManager.RemoveClaimAsync(user, c);

			var ids = moduleIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
			var value = JsonSerializer.Serialize(ids);

			await _userManager.AddClaimAsync(user, new Claim(claimType, value));
			await _signInManager.RefreshSignInAsync(user);

			return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
				? Ok()
				: RedirectToAction("Index", "Courses");
		}
	}
}