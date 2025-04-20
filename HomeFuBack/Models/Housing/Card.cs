using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class Card
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int LocationId { get; set; }
        public Location Location { get; set; }  // Локация(отдельный модель)

        public List<CardCategory> CardCategories { get; set; }  // Категории(отдельный модель)

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }  // Дата начала

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }  // Дата конца

        [Range(1, 5)]
        public int? Rating { get; set; }  // Оценка

        public List<CardImage> Images { get; set; }  // Изображения(список с ссылками)

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }  // Цена

        public bool IsDeleted { get; set; }  // Удален (。_。)

        public Card()
        {
            CardCategories = new List<CardCategory>();
            Images = new List<CardImage>();
            IsDeleted = false;
        }
    }
}
