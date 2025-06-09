using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class Rating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Связь с CardDetail (одна детальная карточка имеет одну запись с агрегированными оценками)
        public int CardDetailId { get; set; }
        [ForeignKey("CardDetailId")]
        public CardDetail CardDetail { get; set; }

        // Оценки
        [Range(0.0, 5.0)]
        public double Cleanliness { get; set; }

        [Range(0.0, 5.0)]
        public double Accuracy { get; set; }

        [Range(0.0, 5.0)]
        public double CheckIn { get; set; } // Прибытие

        [Range(0.0, 5.0)]
        public double Communication { get; set; }

        [Range(0.0, 5.0)]
        public double Location { get; set; }

        [Range(0.0, 5.0)]
        public double Value { get; set; } // Соотношение цена/качество

        [NotMapped] // Не маппится в базу данных
        public double OverallRating => (Cleanliness + Accuracy + CheckIn + Communication + Location + Value) / 6.0;
    }
}
