// HomeFuBack.Models.Housing/Card.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Убедитесь, что это using присутствует

namespace HomeFuBack.Models.Housing
{
    public class Card
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Id генерируется базой данных (по умолчанию, но явно лучше)
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!; // Добавьте null- forgiving operator или инициализируйте

        public int LocationId { get; set; }
        public Location Location { get; set; } = null!; // Добавьте null- forgiving operator или инициализируйте

        public List<CardCategory> CardCategories { get; set; } // Инициализируется в конструкторе

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; } // Это общее свойство Rating для Card, а не Rating-сущность

        public List<string> ImageUrls { get; set; } // Инициализируется в конструкторе

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")] // Рекомендуется для decimal для точности в БД
        public decimal Price { get; set; }

        // УДАЛЯЕМ: CardDetailId и CardDetail
        // Эти навигационные свойства будут управляться со стороны CardDetail для Shared Primary Key
        // public int? CardDetailId { get; set; }
        // [ForeignKey("CardDetailId")]
        // public CardDetail? CardDetail { get; set; } // Навигационное свойство

        // Добавляем навигационное свойство для Shared Primary Key связи (обратная ссылка)
        public CardDetail? CardDetail { get; set; } // Card может иметь один CardDetail

        public bool IsDeleted { get; set; }

        public Card()
        {
            CardCategories = new List<CardCategory>();
            ImageUrls = new List<string>();
            IsDeleted = false;
        }
    }
}