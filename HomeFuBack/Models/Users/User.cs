using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Users

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



        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public DateTime? BirthDate { get; set; }

        public string? Gender { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}
