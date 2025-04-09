namespace HomeFuBack.Data.DAL
{
	public class ContentDao
	{
		private readonly Object _dblocker;
		private readonly ApplicationDbContext _context;
		public ContentDao(ApplicationDbContext context, object dblocker)
		{
			_context = context;
			_dblocker = dblocker;
		}
	}
}
