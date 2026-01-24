using System.ComponentModel.DataAnnotations;

namespace CrudDemo.Models
{
	public class RegisterViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		[DataType(DataType.Password)]
		[Compare("Password", ErrorMessage = "Passwords do not match.")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}

	public class LoginViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;

		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }
	}

	public class ResetPasswordViewModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; } = string.Empty;

		[DataType(DataType.Password)]
		[Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas.")]
		public string ConfirmPassword { get; set; } = string.Empty;

		[Required]
		public string Token { get; set; } = string.Empty;
	}
}
