using HomeFuBack.Data.Entities;
using HomeFuBack.Models.Housing; // Возможно, это пространство имен для Location, Category, Card
using HomeFuBack.Models.Users; // Для User
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Models; // Добавьте это, чтобы получить доступ к Amenity, CardDetail, Rating, CardDetailAmenity

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

        // Добавляем новые DbSet для моделей CardDetail, Amenity, Rating и промежуточной таблицы
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

            // Настройка связи Token с User (уже есть, но можно уточнить)
            modelBuilder.Entity<Entities.Token>() // Используйте полное имя, если есть конфликт
                .HasOne(t => t.User)
                .WithMany() // User может иметь много токенов
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя, удалять его токены

            // --- НОВЫЕ НАСТРОЙКИ СВЯЗЕЙ ---

            // 1. Настройка связи один-к-одному между Card и CardDetail
            // Предполагаем, что Id CardDetail будет совпадать с Id Card, и CardDetailId в Card ссылается на CardDetail
            modelBuilder.Entity<Card>()
                .HasOne(c => c.CardDetail) // Card имеет один CardDetail
                .WithOne() // CardDetail имеет одну Card (нет навигационного свойства обратно, если не нужно)
                .HasForeignKey<Card>("CardDetailId") // Внешний ключ находится в модели Card (поле CardDetailId)
                .IsRequired(false); // Делаем его необязательным, если Card может существовать без CardDetail изначально

            // Альтернативная настройка 1-к-1, если Id CardDetail является первичным ключом и внешним ключом к Card.Id
            // modelBuilder.Entity<CardDetail>()
            //     .HasOne(cd => cd.Card)
            //     .WithOne(c => c.CardDetail)
            //     .HasForeignKey<CardDetail>(cd => cd.Id); // Id CardDetail - FK к Card.Id

            // 2. Настройка связи один-к-одному между CardDetail и Rating
            modelBuilder.Entity<CardDetail>()
                .HasOne(cd => cd.Ratings) // CardDetail имеет одну Rating
                .WithOne(r => r.CardDetail) // Rating имеет одну CardDetail
                .HasForeignKey<Rating>(r => r.CardDetailId); // CardDetailId в Rating является внешним ключом

            // 3. Настройка связи многие-ко-многим между CardDetail и Amenity
            modelBuilder.Entity<CardDetailAmenity>()
                .HasKey(cda => new { cda.CardDetailId, cda.AmenityId }); // Составной первичный ключ

            modelBuilder.Entity<CardDetailAmenity>()
                .HasOne(cda => cda.CardDetail)
                .WithMany(cd => cd.CardDetailAmenities) // CardDetail имеет коллекцию CardDetailAmenity
                .HasForeignKey(cda => cda.CardDetailId);

            modelBuilder.Entity<CardDetailAmenity>()
                .HasOne(cda => cda.Amenity)
                .WithMany() // Amenity не обязательно имеет навигационное свойство к CardDetailAmenities
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