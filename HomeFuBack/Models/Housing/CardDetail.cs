using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using HomeFuBack.Models.Users;


namespace HomeFuBack.Models.Housing
{
    public class CardDetail
    {
        [Key]
        public int Id { get; set; } // Id будет совпадать с Id Card, если это 1-к-1 связь

        // Общая информация
        public int NumberOfGuests { get; set; }
        public int NumberOfBedrooms { get; set; }
        public int NumberOfBeds { get; set; }
        public int NumberOfBathrooms { get; set; }

        // Связь с хозяином
        [Required]
        public Guid HostId { get; set; }
        [ForeignKey("HostId")]
        public User Host { get; set; } // Навигационное свойство для связи с моделью User

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; }

        // Координаты
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Связь один-к-одному с Rating
        // Если у каждой CardDetail есть Rating, это можно сделать не nullable
        public int? RatingId { get; set; } // Foreign Key для Rating
        [ForeignKey("RatingId")]
        public Rating? Ratings { get; set; } // Навигационное свойство

        // Связь со списком удобств (многие-ко-многим)
        public ICollection<CardDetailAmenity> CardDetailAmenities { get; set; }
    }
}
