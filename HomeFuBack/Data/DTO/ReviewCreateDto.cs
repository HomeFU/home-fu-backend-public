using System;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class ReviewCreateDto 
    {
        [Required(ErrorMessage = "Текст комментария обязателен.")]
        [StringLength(500, ErrorMessage = "Текст комментария не может превышать 500 символов.")]
        public string Text { get; set; }

        // Поля оценок, теперь они часть отзыва
        [Range(0.0, 5.0, ErrorMessage = "Оценка чистоты должна быть от 0.0 до 5.0.")]
        // Можно сделать эти поля Required, если все оценки обязательны для отзыва:
        // [Required(ErrorMessage = "Оценка чистоты обязательна.")]
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