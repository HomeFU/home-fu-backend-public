using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeFuBack.Models.Housing
{
    public class Card
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id генерируется базой данных 
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public int LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public List<CardCategory> CardCategories { get; set; } // Инициализируется в конструкторе

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; } // Это общее свойство Rating для Card, а не Rating-сущность

        public List<string> ImageUrls { get; set; } // Инициализируется в конструкторе

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        // Добавляем навигационное свойство для Shared Primary Key связи (обратная ссылка)
        public CardDetail? CardDetail { get; set; } // Card может иметь один CardDetail

        public bool IsDeleted { get; set; }

        public Card()
        {
            CardCategories = [];
            ImageUrls = [];
            IsDeleted = false;
        }
    }
}