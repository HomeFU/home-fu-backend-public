using HomeFuBack.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeFuBack.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		public DbSet<Entities.User> Users { get; set; }
		public DbSet<Entities.Token> Tokens { get; set; }
	}
}