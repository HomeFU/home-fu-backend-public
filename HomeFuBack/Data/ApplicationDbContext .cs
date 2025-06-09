using HomeFuBack.Data.Entities;
using HomeFuBack.Models.Housing; // Для Location, Category, Card, CardDetail, Rating, CardDetailAmenity
using HomeFuBack.Models.Users; // Для User
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Models; // Если Amenity, CardDetail, Rating, CardDetailAmenity находятся здесь

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

        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<CardDetail> CardDetails { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<CardDetailAmenity> CardDetailAmenities { get; set; }
        public DbSet<Reservation> Reservations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка для связи многие-ко-многим между Card и Category
            modelBuilder.Entity<CardCategory>()
                .HasKey(cc => new { cc.CardId, cc.CategoryId });

            // Настройка связи Token с User
            modelBuilder.Entity<Entities.Token>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- КОРРЕКТНЫЕ НАСТРОЙКИ СВЯЗЕЙ ---

            // 1. Настройка связи один-к-одному между Card и CardDetail (Shared Primary Key)
            // Id CardDetail является первичным ключом CardDetail И внешним ключом к Card.Id
            modelBuilder.Entity<Card>()
                .HasOne(c => c.CardDetail) // Card имеет один CardDetail (навигационное свойство в Card)
                .WithOne(cd => cd.Card)    // CardDetail относится к одной Card (навигационное свойство в CardDetail)
                                           // Указываем, что CardDetail.Id является внешним ключом к Card.
                                           // ВАЖНО: ForeignKey здесь относится к зависимой сущности (CardDetail).
                .HasForeignKey<CardDetail>(cd => cd.Id); // CardDetail.Id - это и PK, и FK к Card.Id


            // 2. Настройка связи один-к-одному между CardDetail и Rating
            // CardDetail является принципалом, Rating - зависимая сущность.
            // Rating.CardDetailId является внешним ключом к CardDetail.Id.
            modelBuilder.Entity<CardDetail>()
                .HasOne(cd => cd.Ratings)      // CardDetail имеет один Rating (навигационное свойство в CardDetail)
                .WithOne(r => r.CardDetail)    // Rating относится к одному CardDetail (навигационное свойство в Rating)
                                               // Указываем, что Rating.CardDetailId является внешним ключом к CardDetail.
                .HasForeignKey<Rating>(r => r.CardDetailId);

            // 3. Настройка связи многие-ко-многим между CardDetail и Amenity
            modelBuilder.Entity<CardDetailAmenity>()
                .HasKey(cda => new { cda.CardDetailId, cda.AmenityId }); // Составной первичный ключ

            modelBuilder.Entity<CardDetailAmenity>()
                .HasOne(cda => cda.CardDetail)
                .WithMany(cd => cd.CardDetailAmenities)
                .HasForeignKey(cda => cda.CardDetailId);

            modelBuilder.Entity<CardDetailAmenity>()
                .HasOne(cda => cda.Amenity)
                .WithMany() // Amenity может иметь много CardDetailAmenity, но не обязательно имеет обратную ссылку
                .HasForeignKey(cda => cda.AmenityId);

            // 4. Настройка связи HostId в CardDetail к User
            modelBuilder.Entity<CardDetail>()
                .HasOne(cd => cd.Host)
                .WithMany() // User может быть хостом для многих CardDetails
                .HasForeignKey(cd => cd.HostId)
                .OnDelete(DeleteBehavior.Restrict); // Предотвратить каскадное удаление хозяина при удалении CardDetail

            // 5. Связь Reservation с User (один User - много Reservations)
            modelBuilder.Entity<Reservation>()
                .HasOne<User>() // Убедись, что тип User правильный (из HomeFuBack.Models.Users.User)
                .WithMany()     // Если в модели User есть public ICollection<Reservation> Reservations { get; set; },
                                // то используй .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .IsRequired();  // Так как UserId в Reservation помечен [Required]

            // 6. Связь Reservation с Card (одна Card - много Reservations)
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Card) // Используется навигационное свойство Card в Reservation
                .WithMany()      // Если в модели Card есть public ICollection<Reservation> Reservations { get; set; },
                                 // то используй .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CardId)
                .IsRequired();   // Так как CardId в Reservation помечен [Required]
        }
    }
}