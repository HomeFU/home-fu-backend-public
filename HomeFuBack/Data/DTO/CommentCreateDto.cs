using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class CommentCreateDto
    {
        [Required(ErrorMessage = "Текст комментария обязателен.")]
        [StringLength(500, ErrorMessage = "Текст комментария не может превышать 500 символов.")]
        public string Text { get; set; }

        [Required(ErrorMessage = "ID пользователя обязателен.")]
        public Guid UserId { get; set; } // ID пользователя, оставляющего комментарий
    }
}