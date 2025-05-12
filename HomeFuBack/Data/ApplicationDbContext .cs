using HomeFuBack.Data.Entities;
using HomeFuBack.Models.Housing;
using HomeFuBack.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace HomeFuBack.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Entities.Token> Tokens { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardCategory> CardsCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CardCategory>()
                .HasKey(cc => new { cc.CardId, cc.CategoryId });

            modelBuilder.Entity<Token>()
                .HasOne(t => t.User)
                .WithMany() 
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}