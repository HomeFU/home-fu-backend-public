using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HomeFuBack.Data.DTO
{
    // DTO для создания удобства
    public class AmenityDto
    {
        [Required(ErrorMessage = "Название удобства обязательно.")]
        [StringLength(100, ErrorMessage = "Название удобства не может превышать 100 символов.")]
        public string Name { get; set; }

        public IFormFile? ImageFile { get; set; } // Для загрузки файла изображения
    }

    // DTO для обновления удобства
    public class AmenityUpdateDto
    {
        [StringLength(100, ErrorMessage = "Название удобства не может превышать 100 символов.")]
        public string? Name { get; set; } // Опционально для обновления

        public IFormFile? ImageFile { get; set; } // Опционально для обновления изображения

        public bool RemoveImage { get; set; } = false; // Флаг для удаления существующего изображения
    }
}