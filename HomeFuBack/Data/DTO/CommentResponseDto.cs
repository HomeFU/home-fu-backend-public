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
        public string UserName { get; set; } // Имя пользователя, оставившего комментарий
        public string? UserProfileImageUrl { get; set; } // Аватар пользователя
    }
}