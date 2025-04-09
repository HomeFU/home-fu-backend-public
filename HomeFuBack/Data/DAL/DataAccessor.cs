namespace HomeFuBack.Data.DAL
{
	public class DataAccessor
	{
		private readonly Object _dblocker = new Object();

		private readonly ApplicationDbContext _dataContext;

		public UserDao UserDao { get; private set; }
		public ContentDao ContentDao { get; private set; }
		public DataAccessor(ApplicationDbContext dataContext)
		{
			_dataContext = dataContext;
			UserDao = new UserDao(dataContext, _dblocker);
			ContentDao = new(_dataContext, _dblocker);
		}
	}
}
