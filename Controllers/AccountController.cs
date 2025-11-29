using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CrudDemo.Models;

namespace CrudDemo.Controllers
{
	public class AccountController : Controller
	{
			private readonly UserManager<IdentityUser> _userManager;
			private readonly SignInManager<IdentityUser> _signInManager;
			private readonly RoleManager<IdentityRole> _roleManager;

			public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
			{
				_userManager = userManager;
				_signInManager = signInManager;
				_roleManager = roleManager;
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
					return RedirectToAction("Index", "Home");
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
	}
}
