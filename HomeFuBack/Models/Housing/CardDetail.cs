using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using HomeFuBack.Models.Users; // Для User

namespace HomeFuBack.Models.Housing
{
    public class CardDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Id не автогенерируется
        public int Id { get; set; }

        // Связь один-к-одному с Card (Shared Primary Key)
        // Id CardDetail является первичным ключом CardDetail И внешним ключом к Card.
        [ForeignKey("Id")] // Указывает, что Id CardDetail является внешним ключом к Card.
        public Card Card { get; set; } = null!; // Навигационное свойство к Card

        // Общая информация
        public int NumberOfGuests { get; set; }
        public int NumberOfBedrooms { get; set; }
        public int NumberOfBeds { get; set; }
        public int NumberOfBathrooms { get; set; }

        // Связь с хозяином
        [Required]
        public Guid HostId { get; set; }
        [ForeignKey("HostId")]
        public User Host { get; set; } = null!; // Навигационное свойство для связи с моделью User

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; }

        // Координаты
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Добавляем навигационное свойство для 1:1 связи, где Rating является зависимой сущностью
        public Rating? Ratings { get; set; } // CardDetail может иметь один Rating

        // Связь со списком удобств (многие-ко-многим)
        public ICollection<CardDetailAmenity> CardDetailAmenities { get; set; } = new List<CardDetailAmenity>(); // Инициализация
    }
}