namespace HomeFuBack.Data.Entities
{
	public class Token
	{
		public Guid id { get; set; }
		public Guid UserId { get; set	; }
		public DateTime SubmitDt { get; set; }
		public DateTime ExpireDt { get; set; }

		public User User { get; set; }
	}
}
