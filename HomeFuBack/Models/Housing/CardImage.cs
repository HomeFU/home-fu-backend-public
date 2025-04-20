using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class CardImage
    {
        [Key]
        public int Id { get; set; }

        public int CardId { get; set; }
        public Card Card { get; set; }

        [Required]
        public string ImageUrl { get; set; }  // Ссылка на изображение
    }
}
