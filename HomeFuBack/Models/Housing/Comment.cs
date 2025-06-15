using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeFuBack.Models.Users;

namespace HomeFuBack.Models.Housing
{
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)] // Максимальная длина комментария
        public string Text { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Автоматически устанавливаем время создания

        // Связь с CardDetail (один CardDetail может иметь много комментариев)
        [Required]
        public int CardDetailId { get; set; }
        [ForeignKey("CardDetailId")]
        public CardDetail CardDetail { get; set; } = null!; // Навигационное свойство к CardDetail

        // Связь с пользователем, оставившим комментарий (один User может оставить много комментариев)
        [Required]
        public Guid UserId { get; set; } // Предполагается, что UserId в User - это Guid
        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // Навигационное свойство к User
    }
}