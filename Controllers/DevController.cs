using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CrudDemo.Controllers
{
	/// <summary>
	/// Development-only helper controller. Only active in Development environment.
	/// </summary>
	public class DevController : Controller
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IWebHostEnvironment _env;

		public DevController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_env = env;
		}

		[HttpGet("/dev/make-first-user-admin")]
		public async Task<IActionResult> MakeFirstUserAdmin()
		{
			if (!_env.IsDevelopment())
				return Forbid();

			var firstUser = _userManager.Users.FirstOrDefault();
			if (firstUser == null)
				return Content("No users found in the database.");

			const string adminRole = "Admin";
			if (!await _roleManager.RoleExistsAsync(adminRole))
			{
				await _roleManager.CreateAsync(new IdentityRole(adminRole));
			}

			if (await _userManager.IsInRoleAsync(firstUser, adminRole))
				return Content($"User '{firstUser.Email}' is already Admin.");

			await _userManager.AddToRoleAsync(firstUser, adminRole);
			return Content($"User '{firstUser.Email}' is now Admin. Refresh your browser and log in again.");
		}
	}
}
