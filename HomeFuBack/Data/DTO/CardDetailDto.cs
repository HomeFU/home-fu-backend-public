using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class CardDetailDto
    {
        // Не включаем Id, так как он будет установлен при создании/обновлении

        [Range(1, 20)]
        public int NumberOfGuests { get; set; }

        [Range(1, 10)]
        public int NumberOfBedrooms { get; set; }

        [Range(1, 20)]
        public int NumberOfBeds { get; set; }

        [Range(0, 10)]
        public int NumberOfBathrooms { get; set; }

        [Required]
        [MinLength(50)]
        public string Description { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public List<int>? AmenityIds { get; set; } = new List<int>();

        // Оценки теперь являются объектом RatingDto
        public RatingDto? Ratings { get; set; } // Здесь необязательно, если Rating может быть создан позже
    }

    // RatingDto остается таким же
    public class RatingDto
    {
        [Range(0.0, 5.0)]
        public double Cleanliness { get; set; }
        [Range(0.0, 5.0)]
        public double Accuracy { get; set; }
        [Range(0.0, 5.0)]
        public double CheckIn { get; set; }
        [Range(0.0, 5.0)]
        public double Communication { get; set; }
        [Range(0.0, 5.0)]
        public double Location { get; set; }
        [Range(0.0, 5.0)]
        public double Value { get; set; }
    }
}