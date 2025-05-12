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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCard(int id, CardDto cardDto)
        {
            if (id != cardDto.Id)
            {
                return BadRequest();
            }

            if (!await _context.Locations.AnyAsync(l => l.Id == cardDto.LocationId))
            {
                return BadRequest("Invalid LocationId");
            }

            var existingCard = await _context.Cards
                .Include(c => c.CardCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCard == null)
            {
                return NotFound();
            }

            existingCard.Name = cardDto.Name;
            existingCard.LocationId = cardDto.LocationId;
            existingCard.StartDate = cardDto.StartDate;
            existingCard.EndDate = cardDto.EndDate;
            existingCard.Rating = cardDto.Rating;
            existingCard.Price = cardDto.Price;
            existingCard.IsDeleted = cardDto.IsDeleted;

            if (cardDto.CategoryIds != null)
            {
                existingCard.CardCategories.RemoveAll(cc => !cardDto.CategoryIds.Contains(cc.CategoryId));

                foreach (var categoryId in cardDto.CategoryIds)
                {
                    if (!existingCard.CardCategories.Any(cc => cc.CategoryId == categoryId))
                    {
                        var category = await _context.Categories.FindAsync(categoryId);
                        if (category == null)
                        {
                            return BadRequest($"Category with ID {categoryId} not found.");
                        }
                        existingCard.CardCategories.Add(new CardCategory { CardId = id, CategoryId = categoryId });
                    }
                }
            }

            _context.Entry(existingCard).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CardExists(id))
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