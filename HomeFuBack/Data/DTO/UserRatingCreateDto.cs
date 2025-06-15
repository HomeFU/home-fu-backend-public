using System;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class UserRatingCreateDto
    {
        [Required(ErrorMessage = "ID пользователя обязателен.")]
        public Guid UserId { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка чистоты должна быть от 0.0 до 5.0.")]
        public double? Cleanliness { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка точности должна быть от 0.0 до 5.0.")]
        public double? Accuracy { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка прибытия должна быть от 0.0 до 5.0.")]
        public double? CheckIn { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка коммуникации должна быть от 0.0 до 5.0.")]
        public double? Communication { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка местоположения должна быть от 0.0 до 5.0.")]
        public double? Location { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Оценка соотношения цена/качество должна быть от 0.0 до 5.0.")]
        public double? Value { get; set; }
    }
}