using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Helpers;
using HomeFuBack.Models.Housing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Маршрут будет /api/cards
    public class CardsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CardsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/cards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> GetCards()
        {
            var cards = await _context.Cards
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .Select(c => new CardResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    LocationId = c.LocationId,
                    LocationName = c.Location.Name,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Rating = c.Rating,
                    Price = c.Price,
                    IsDeleted = c.IsDeleted,
                    ImageUrls = c.ImageUrls,
                    CategoryIds = c.CardCategories.Select(cc => cc.CategoryId).ToList()
                })
                .ToListAsync();

            return Ok(cards);
        }

        // GET: api/cards/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CardResponseDto>> GetCard(int id)
        {
            var card = await _context.Cards
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            var responseDto = new CardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                LocationId = card.LocationId,
                LocationName = card.Location?.Name!,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                Rating = card.Rating,
                Price = card.Price,
                IsDeleted = card.IsDeleted,
                ImageUrls = card.ImageUrls,
                CategoryIds = card.CardCategories.Select(cc => cc.CategoryId).ToList() // Получаем ID категорий
            };

            return Ok(responseDto);
        }

        // POST: api/cards
        [HttpPost]
        public async Task<ActionResult<CardResponseDto>> PostCardWithImages([FromForm] CardDto cardDto)
        {
            if (!await _context.Locations.AnyAsync(l => l.Id == cardDto.LocationId))
            {
                return BadRequest("Invalid LocationId");
            }

            var card = new Card
            {
                Name = cardDto.Name,
                LocationId = cardDto.LocationId,
                StartDate = cardDto.StartDate,
                EndDate = cardDto.EndDate,
                Rating = cardDto.Rating,
                Price = cardDto.Price,
                IsDeleted = cardDto.IsDeleted,
                ImageUrls = new List<string>(),
                CardCategories = new List<CardCategory>()
            };

            if (cardDto.Images != null && cardDto.Images.Any())
            {
                foreach (var image in cardDto.Images)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = UniqueFileName.GetUniqueFileName(image.FileName);
                        var imagePath = Path.Combine(_environment.WebRootPath, "images", uniqueFileName);

                        await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        var imageUrl = $"/images/{uniqueFileName}";
                        card.ImageUrls.Add(imageUrl);
                    }
                }
            }

            if (cardDto.CategoryIds != null && cardDto.CategoryIds.Any())
            {
                foreach (var categoryId in cardDto.CategoryIds)
                {
                    var category = await _context.Categories.FindAsync(categoryId);
                    if (category == null)
                    {
                        return BadRequest($"Category with ID {categoryId} not found.");
                    }
                    card.CardCategories.Add(new CardCategory { Card = card, Category = category });
                }
            }

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            await _context.Entry(card)
                .Reference(c => c.Location)
                .LoadAsync();

            var responseDto = new CardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                LocationId = card.LocationId,
                LocationName = card.Location?.Name!,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                Rating = card.Rating,
                Price = card.Price,
                IsDeleted = card.IsDeleted,
                ImageUrls = card.ImageUrls,
                CategoryIds = card.CardCategories.Select(cc => cc.CategoryId).ToList()
            };

            return CreatedAtAction(nameof(GetCard), new { id = card.Id }, responseDto);
        }

        // PUT: api/cards/{id}
        // [Authorize(Roles = "Admin")] // Усли требуется авторизация для администраторов
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCard(int id, CardUpdateDto cardUpdateDto)
        {
            // Проверка, что ID в URL соответствует ID в теле запроса
            if (id != cardUpdateDto.Id)
            {
                return BadRequest("ID в URL не соответствует ID в теле запроса.");
            }

            // Базовая валидация DTO перед началом обработки
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCard = await _context.Cards
                .Include(c => c.CardCategories) // Включаем категории, так как их будем обновлять
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCard == null)
            {
                return NotFound($"Карточка с ID {id} не найдена.");
            }

            // Благодаря nullable-типам в CardUpdateDto, мы можем проверить .HasValue или != null

            if (cardUpdateDto.Name != null)
            {
                existingCard.Name = cardUpdateDto.Name;
            }
            if (cardUpdateDto.LocationId.HasValue)
            {
                // Проверяем LocationId только если он предоставлен
                // и убеждаемся, что такой Location существует
                if (!await _context.Locations.AnyAsync(l => l.Id == cardUpdateDto.LocationId.Value))
                {
                    return BadRequest("Некорректный LocationId: Локация не найдена.");
                }
                existingCard.LocationId = cardUpdateDto.LocationId.Value;
            }
            if (cardUpdateDto.StartDate.HasValue)
            {
                existingCard.StartDate = cardUpdateDto.StartDate.Value;
            }
            if (cardUpdateDto.EndDate.HasValue)
            {
                existingCard.EndDate = cardUpdateDto.EndDate.Value;
            }
            if (cardUpdateDto.Price.HasValue)
            {
                existingCard.Price = cardUpdateDto.Price.Value;
            }
            if (cardUpdateDto.IsDeleted.HasValue)
            {
                existingCard.IsDeleted = cardUpdateDto.IsDeleted.Value;
            }

            // Обновление ImageUrls (предполагаем, что это List<string> в модели Card)
            // Если ImageUrls предоставлены, заменяем существующий список.
            // Если ImageUrls == null, то это поле не меняется.
            // Если ImageUrls = [], то список очищается.
            if (cardUpdateDto.ImageUrls != null)
            {
                existingCard.ImageUrls = cardUpdateDto.ImageUrls;
            }

            // --- Обновление категорий (CardCategories - отношение "многие ко многим") ---
            // Логика обрабатывает добавление новых категорий и удаление отсутствующих.
            if (cardUpdateDto.CategoryIds != null) // Проверяем, что список CategoryIds был предоставлен
            {
                // Получаем текущие ID категорий, связанные с карточкой
                var currentCategoryIds = existingCard.CardCategories.Select(cc => cc.CategoryId).ToList();

                // Идентификаторы категорий, которые нужно добавить (новые ID, которых нет в текущих)
                var categoriesToAdd = cardUpdateDto.CategoryIds.Except(currentCategoryIds).ToList();

                // Идентификаторы категорий, которые нужно удалить (текущие ID, которых нет в новом списке)
                var categoriesToRemove = currentCategoryIds.Except(cardUpdateDto.CategoryIds).ToList();

                // Удаляем CardCategory сущности, которые больше не нужны
                foreach (var categoryIdToRemove in categoriesToRemove)
                {
                    var cardCategoryToRemove = existingCard.CardCategories.FirstOrDefault(cc => cc.CategoryId == categoryIdToRemove);
                    if (cardCategoryToRemove != null)
                    {
                        _context.CardsCategories.Remove(cardCategoryToRemove); // Удаляем из DbSet для отслеживания EF
                    }
                }

                // Добавляем новые CardCategory сущности
                foreach (var categoryIdToAdd in categoriesToAdd)
                {
                    var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryIdToAdd);
                    if (!categoryExists)
                    {
                        return BadRequest($"Категория с ID {categoryIdToAdd} не найдена.");
                    }
                    existingCard.CardCategories.Add(new CardCategory { CardId = id, CategoryId = categoryIdToAdd });
                }
            }

            // --- Поля CardDetail (и CardDetailAmenities) отсутствуют в вашем DTO.
            // --- Поэтому логика для их обновления здесь не включена.
            // --- Если вы захотите их обновлять, вам нужно будет добавить их в CardUpdateDto
            // --- (например, через вложенный CardDetailUpdateDto).

            // Помечаем основную сущность Card как измененную.
            // EF Core автоматически отследит изменения в CardCategories
            // благодаря тому, что мы добавляли/удаляли их из отслеживаемых коллекций и контекста.
            _context.Entry(existingCard).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Обработка конфликтов конкуренции: если карточка была изменена другим пользователем.
                if (!CardExists(id))
                {
                    return NotFound("Карточка не найдена (возможно, была удалена другим пользователем).");
                }
                else
                {
                    // Это означает, что кто-то изменил карточку между чтением и сохранением.
                    // Вы можете логировать эту ошибку или предоставить более детальную информацию
                    // клиенту (например, отправить текущую версию карточки).
                    throw; // Перебрасываем исключение, чтобы оно было поймано глобальным обработчиком ошибок или отлажено.
                }
            }
            catch (Exception ex) // Общий обработчик для других возможных ошибок при сохранении
            {
                // Здесь вы можете использовать ILogger для логирования ошибки:
                // _logger.LogError(ex, "Ошибка при сохранении карточки с ID {CardId}", id);
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }

            return NoContent(); // Успешно обновлено, нет содержимого для возврата
        }

        // DELETE: api/cards/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }


        // GET: api/cards/byCategory?categoryIds=1&categoryIds=3&categoryIds=5
        [HttpGet("byCategory")]
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> GetCardsByCategory([FromQuery] List<int> categoryIds)
        {
            if (categoryIds == null || !categoryIds.Any())
            {
                return BadRequest("Please provide at least one category ID.");
            }

            var cards = await _context.Cards
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .Where(c => c.CardCategories.Any(cc => categoryIds.Contains(cc.CategoryId)))
                .Select(c => new CardResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    LocationId = c.LocationId,
                    LocationName = c.Location.Name,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Rating = c.Rating,
                    Price = c.Price,
                    IsDeleted = c.IsDeleted,
                    ImageUrls = c.ImageUrls,
                    CategoryIds = c.CardCategories.Select(cc => cc.CategoryId).ToList()
                })
                .ToListAsync();

            if (!cards.Any())
            {
                return NotFound($"No cards found for the specified category IDs: {string.Join(", ", categoryIds)}");
            }

            return Ok(cards);
        }
    }
}