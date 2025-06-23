using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Models.Housing; 
using HomeFuBack.Data.DTO;
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
    public class AmenitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AmenitiesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/amenities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Amenity>>> GetAmenities()
        {
            return await _context.Amenities.ToListAsync();
        }

        // GET: api/amenities/{id}
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
            else if (amenityUpdateDto.RemoveImage)
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
                }
            }
        }
    }
}