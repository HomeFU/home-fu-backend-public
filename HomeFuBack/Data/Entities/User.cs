using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.Entities

{
	public class User
	{
		public Guid Id { get; set; }

		[Required, EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Password { get; set; }

		public string? EmailConfirmCode { get; set; }

		public string? Role { get; set; }
	}
}
