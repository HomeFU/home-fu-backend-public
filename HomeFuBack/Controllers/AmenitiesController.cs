using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Models.Housing; // Убедитесь, что ваша модель Amenity находится здесь
using HomeFuBack.Data.DTO; // Если вы используете AmenityDto для входных данных
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System; // Для Guid

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/amenities")]
    // [Authorize(Roles = "Admin")] // Возможно, доступ к управлению удобствами будет только у администраторов
    public class AmenitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Переименовал в _environment для соответствия CategoryController

        public AmenitiesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/amenities
        /// <summary>
        /// Получает список всех удобств.
        /// </summary>
        /// <returns>Список объектов Amenity.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Amenity>>> GetAmenities()
        {
            return await _context.Amenities.ToListAsync();
        }

        // GET: api/amenities/{id}
        /// <summary>
        /// Получает удобство по его ID.
        /// </summary>
        /// <param name="id">ID удобства.</param>
        /// <returns>Объект Amenity или NotFound, если удобство не найдено.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Amenity>> GetAmenity(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);

            if (amenity == null)
            {
                return NotFound();
            }

            return amenity;
        }

        // POST: api/amenities
        /// <summary>
        /// Создает новое удобство с опциональным файлом изображения.
        /// </summary>
        /// <param name="amenityDto">Данные для создания удобства, включая файл изображения.</param>
        /// <returns>Созданное удобство.</returns>
        [HttpPost]
        public async Task<ActionResult<Amenity>> PostAmenity([FromForm] AmenityDto amenityDto)
        {
            if (string.IsNullOrWhiteSpace(amenityDto.Name))
            {
                return BadRequest("Название удобства обязательно.");
            }

            // Проверка на уникальность имени
            if (await _context.Amenities.AnyAsync(a => a.Name == amenityDto.Name))
            {
                return BadRequest("Удобство с таким именем уже существует.");
            }

            var amenity = new Amenity { Name = amenityDto.Name };

            // Добавляем удобство и сохраняем, чтобы получить ID для имени файла
            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();

            if (amenityDto.ImageFile != null && amenityDto.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg" };
                var fileExtension = Path.GetExtension(amenityDto.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    // Если формат не поддерживается, удаляем ранее сохраненное удобство (если его ID уже присвоен)
                    // или обрабатываем ошибку. Для простоты сейчас просто возвращаем BadRequest.
                    _context.Amenities.Remove(amenity); // Откатываем добавление
                    await _context.SaveChangesAsync();
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                var uniqueFileName = $"amenity_{amenity.Id}_{Guid.NewGuid()}{fileExtension}";
                var imageDirectory = Path.Combine(_environment.WebRootPath, "images", "amenities");
                if (!Directory.Exists(imageDirectory))
                {
                    Directory.CreateDirectory(imageDirectory);
                }
                var imagePath = Path.Combine(imageDirectory, uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await amenityDto.ImageFile.CopyToAsync(fileStream);
                }

                // Сохраняем относительный URL в базу данных
                amenity.IconPath = $"/images/amenities/{uniqueFileName}";
                _context.Update(amenity); // Обновляем категорию с новым URL
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetAmenity), new { id = amenity.Id }, amenity);
        }

        // PUT: api/amenities/{id}
        /// <summary>
        /// Обновляет существующее удобство. Можно обновить имя и/или изображение.
        /// </summary>
        /// <param name="id">ID удобства для обновления.</param>
        /// <param name="amenityUpdateDto">Обновленные данные удобства.</param>
        /// <returns>NoContent, если обновление успешно, или BadRequest/NotFound.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAmenity(int id, [FromForm] AmenityUpdateDto amenityUpdateDto)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }

            // Обновляем имя, если оно передано и не пустое
            if (!string.IsNullOrWhiteSpace(amenityUpdateDto.Name))
            {
                // Проверка на уникальность имени, исключая текущую запись
                if (amenity.Name != amenityUpdateDto.Name && await _context.Amenities.AnyAsync(a => a.Id != id && a.Name == amenityUpdateDto.Name))
                {
                    return BadRequest("Удобство с таким именем уже существует.");
                }
                amenity.Name = amenityUpdateDto.Name;
            }

            // Обработка файла изображения
            if (amenityUpdateDto.ImageFile != null && amenityUpdateDto.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg" };
                var fileExtension = Path.GetExtension(amenityUpdateDto.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                // Удаляем старое изображение, если оно есть
                if (!string.IsNullOrEmpty(amenity.IconPath))
                {
                    DeleteAmenityImage(amenity.IconPath);
                }

                var uniqueFileName = $"amenity_{id}_{Guid.NewGuid()}{fileExtension}";
                var imageDirectory = Path.Combine(_environment.WebRootPath, "images", "amenities");
                if (!Directory.Exists(imageDirectory))
                {
                    Directory.CreateDirectory(imageDirectory);
                }
                var imagePath = Path.Combine(imageDirectory, uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await amenityUpdateDto.ImageFile.CopyToAsync(fileStream);
                }

                amenity.IconPath = $"/images/amenities/{uniqueFileName}";
            }
            else if (amenityUpdateDto.RemoveImage) // Если флаг RemoveImage установлен и нет нового файла
            {
                if (!string.IsNullOrEmpty(amenity.IconPath))
                {
                    DeleteAmenityImage(amenity.IconPath);
                    amenity.IconPath = null; // Обнуляем URL в БД
                }
            }


            _context.Update(amenity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AmenityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/amenities/{id}
        /// <summary>
        /// Удаляет удобство по его ID и связанный файл изображения.
        /// </summary>
        /// <param name="id">ID удобства для удаления.</param>
        /// <returns>NoContent, если удаление успешно, или NotFound.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }

            // Удаляем связанный файл изображения
            if (!string.IsNullOrEmpty(amenity.IconPath))
            {
                DeleteAmenityImage(amenity.IconPath);
            }

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/amenities/{id}/image
        /// <summary>
        /// Загружает или обновляет изображение для существующего удобства.
        /// </summary>
        /// <param name="id">ID удобства.</param>
        /// <param name="file">Файл изображения.</param>
        /// <returns>URL загруженного изображения.</returns>
        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadAmenityImage(int id, IFormFile file)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }

            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                // Удаляем старое изображение, если оно есть
                if (!string.IsNullOrEmpty(amenity.IconPath))
                {
                    DeleteAmenityImage(amenity.IconPath);
                }

                var uniqueFileName = $"amenity_{id}_{Guid.NewGuid()}{fileExtension}";
                var imageDirectory = Path.Combine(_environment.WebRootPath, "images", "amenities");
                if (!Directory.Exists(imageDirectory))
                {
                    Directory.CreateDirectory(imageDirectory);
                }
                var imagePath = Path.Combine(imageDirectory, uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                amenity.IconPath = $"/images/amenities/{uniqueFileName}";
                _context.Update(amenity);
                await _context.SaveChangesAsync();

                return Ok(new { IconPath = amenity.IconPath });
            }

            return BadRequest("Файл не был загружен или является пустым.");
        }


        private bool AmenityExists(int id)
        {
            return _context.Amenities.Any(e => e.Id == id);
        }

        // Вспомогательный метод для удаления файла по относительному пути
        private void DeleteAmenityImage(string relativeIconPath)
        {
            if (string.IsNullOrEmpty(relativeIconPath)) return;

            // Удаляем начальный слеш, если он есть
            var filePathOnDisk = Path.Combine(_environment.WebRootPath, relativeIconPath.TrimStart('/'));

            if (System.IO.File.Exists(filePathOnDisk))
            {
                try
                {
                    System.IO.File.Delete(filePathOnDisk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении изображения {filePathOnDisk}: {ex.Message}");
                    // Здесь можно добавить логирование
                }
            }
        }
    }
}