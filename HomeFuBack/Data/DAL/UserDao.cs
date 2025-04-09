using HomeFuBack.Data.Entities;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;


namespace HomeFuBack.Data.DAL
{
	public class UserDao
	{
		private readonly Object _dblocker;
		private readonly ApplicationDbContext _dataContext;

		public UserDao(ApplicationDbContext dataContext, object dblocker)
		{
			_dataContext = dataContext;
			_dblocker = dblocker;
		}

		public User? GetUserById(String id)
		{
			User? user;
			try
			{
				lock (_dblocker)
				{
					user = _dataContext.Users.Find(Guid.Parse(id));
				}
			}
			catch { return null; }
			return user;
		}
		public User? GetUserByToken(Guid token)
		{
			User? user;
			lock (_dblocker)
			{
				user = _dataContext.Tokens
					.Include(t => t.User)
					.FirstOrDefault(t => t.id == token)
					?.User;
			}
			return user;
		}

		public Token? FindUserToken(User user)
		{
			Token? userToken = _dataContext.Tokens.FirstOrDefault(t => t.UserId == user.Id);
			DateTime today = DateTime.Now;
			if (userToken == null || userToken.ExpireDt < today)
			{
				return null;
			}
			return userToken;
		}

		public Token? CheckExpiredToken(Guid tokenId)
		{
			Token? token = _dataContext.Tokens.FirstOrDefault(t => t.id == tokenId);
			DateTime today = DateTime.Now;
			if (token != null && token.ExpireDt < today)
			{
				return null;
			}
			return token;
		}

		public Token? CreateTokenForUser(User user)
		{
			return CreateTokenForUser(user.Id);
		}
		public Token? CreateTokenForUser(Guid userId)
		{
			Token token = new()
			{
				id = Guid.NewGuid(),
				UserId = userId,
				SubmitDt = DateTime.Now,
				ExpireDt = DateTime.Now.AddDays(1)
			};
			_dataContext.Tokens.Add(token);
			try
			{
				lock (_dblocker)
				{
					_dataContext.SaveChanges();
				}
				return token;
			}
			catch
			{
				_dataContext.Tokens.Remove(token);
				return null;
			}
		}

		public void Signup(User user)
		{
			if (user.Id == default)
			{
				user.Id = Guid.NewGuid();
			}
			_dataContext.Users.Add(user);
			_dataContext.SaveChanges();
		}
	}
}
