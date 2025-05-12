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
        public Location Location { get; set; }

        public List<CardCategory> CardCategories { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        public List<string> ImageUrls { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public bool IsDeleted { get; set; }

        public Card()
        {
            CardCategories = new List<CardCategory>();
            ImageUrls = new List<string>();
            IsDeleted = false;
        }
    }
}