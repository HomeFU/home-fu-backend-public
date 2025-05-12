using HomeFuBack.Models.Users;

namespace HomeFuBack.Data.Entities
{
	public class Token
	{
		public Guid id { get; set; }
		public Guid UserId { get; set	; }
		public DateTime SubmitDt { get; set; }
		public DateTime ExpireDt { get; set; }


        // Добавляем поля для хранения токенов
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpireDt { get; set; }

        public User User { get; set; }

    }
}
