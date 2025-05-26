using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory([FromForm] CategoryDto categoryDto)
        {
            if (string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return BadRequest("Название категории обязательно.");
            }

            var category = new Category { Name = categoryDto.Name };

            if (categoryDto.ImageFile != null && categoryDto.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg" };
                var fileExtension = Path.GetExtension(categoryDto.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync(); // Сохраняем для получения ID

                var uniqueFileName = $"category_{category.Id}_{Guid.NewGuid()}{fileExtension}";
                var imagePath = Path.Combine(_environment.WebRootPath, "images/categories", uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await categoryDto.ImageFile.CopyToAsync(fileStream);
                }

                category.ImageUrl = $"/images/categories/{uniqueFileName}";
                _context.Update(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            else
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, [FromForm] CategoryUpdateDto categoryUpdateDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(categoryUpdateDto.Name))
            {
                category.Name = categoryUpdateDto.Name;
            }

            if (categoryUpdateDto.ImageFile != null && categoryUpdateDto.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".svg" };
                var fileExtension = Path.GetExtension(categoryUpdateDto.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                // Удаляем старое изображение, если оно есть
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, category.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при удалении старого изображения: {ex.Message}");
                        }
                    }
                }

                var uniqueFileName = $"category_{id}_{Guid.NewGuid()}{fileExtension}";
                var imagePath = Path.Combine(_environment.WebRootPath, "images/categories", uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await categoryUpdateDto.ImageFile.CopyToAsync(fileStream);
                }

                category.ImageUrl = $"/images/categories/{uniqueFileName}";
            }

            _context.Update(category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/categories/{id}/image
        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadCategoryImage(int id, IFormFile file)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif" , ".svg"};
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                var uniqueFileName = $"category_{id}_{Guid.NewGuid()}{fileExtension}";
                var imagePath = Path.Combine(_environment.WebRootPath, "images/categories", uniqueFileName);

                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                category.ImageUrl = $"/images/categories/{uniqueFileName}";
                _context.Update(category);
                await _context.SaveChangesAsync();

                return Ok(new { imageUrl = category.ImageUrl });
            }

            return BadRequest("Файл не был загружен или является пустым.");
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}