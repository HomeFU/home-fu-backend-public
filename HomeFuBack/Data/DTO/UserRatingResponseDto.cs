using System;

namespace HomeFuBack.Data.DTO
{
    public class UserRatingResponseDto
    {
        public int Id { get; set; }
        public int CardDetailId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } // Имя пользователя
        public string? UserProfileImageUrl { get; set; } // Аватар пользователя
        public DateTime CreatedAt { get; set; }

        public double? Cleanliness { get; set; }
        public double? Accuracy { get; set; }
        public double? CheckIn { get; set; }
        public double? Communication { get; set; }
        public double? Location { get; set; }
        public double? Value { get; set; }
        public double? OverallRating { get; set; }
    }
}