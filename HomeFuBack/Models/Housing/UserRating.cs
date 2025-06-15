using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeFuBack.Models.Users; // Для User

namespace HomeFuBack.Models.Housing
{
    public class UserRating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Связь с CardDetail (одна CardDetail может иметь много пользовательских оценок)
        [Required]
        public int CardDetailId { get; set; }
        [ForeignKey("CardDetailId")]
        public CardDetail CardDetail { get; set; } = null!; // Навигационное свойство к CardDetail

        // Связь с пользователем, оставившим оценку
        [Required]
        public Guid UserId { get; set; } // Предполагается, что UserId в User - это Guid
        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // Навигационное свойство к User

        // Дата создания оценки
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Сами оценки (могут быть nullable, если пользователь не все оценивает)
        [Range(0.0, 5.0)]
        public double? Cleanliness { get; set; }

        [Range(0.0, 5.0)]
        public double? Accuracy { get; set; }

        [Range(0.0, 5.0)]
        public double? CheckIn { get; set; } // Прибытие

        [Range(0.0, 5.0)]
        public double? Communication { get; set; }

        [Range(0.0, 5.0)]
        public double? Location { get; set; }

        [Range(0.0, 5.0)]
        public double? Value { get; set; } // Соотношение цена/качество

        // Можно добавить OverallRating как NotMapped, если нужно
        [NotMapped]
        public double? OverallRating
        {
            get
            {
                var ratings = new List<double>();
                if (Cleanliness.HasValue) ratings.Add(Cleanliness.Value);
                if (Accuracy.HasValue) ratings.Add(Accuracy.Value);
                if (CheckIn.HasValue) ratings.Add(CheckIn.Value);
                if (Communication.HasValue) ratings.Add(Communication.Value);
                if (Location.HasValue) ratings.Add(Location.Value);
                if (Value.HasValue) ratings.Add(Value.Value);

                return ratings.Any() ? ratings.Average() : (double?)null;
            }
        }
    }
}