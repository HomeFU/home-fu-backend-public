using System;

namespace HomeFuBack.Data.DTO
{
    public class CommentResponseDto 
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CardDetailId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string? UserProfileImageUrl { get; set; }

        // Поля оценок
        public double? Cleanliness { get; set; }
        public double? Accuracy { get; set; }
        public double? CheckIn { get; set; }
        public double? Communication { get; set; }
        public double? Location { get; set; }
        public double? Value { get; set; }
        public double? OverallRating { get; set; } // Вычисляемая общая оценка
    }
}