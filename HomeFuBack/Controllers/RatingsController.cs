using HomeFuBack.Data.DTO;
using HomeFuBack.Data;
using HomeFuBack.Models.Housing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/carddetails/{cardDetailId}/ratings")] // Маршрут привязан к CardDetail
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RatingsController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<Rating>> GetRatingByCardDetailId(int cardDetailId)
        {
            // Ищем Rating по CardDetailId
            var rating = await _context.Ratings
                                       .FirstOrDefaultAsync(r => r.CardDetailId == cardDetailId);

            if (rating == null)
            {
                return NotFound($"Оценки для CardDetail с ID {cardDetailId} не найдены.");
            }

            return rating;
        }

        [HttpPut]
        // [Authorize(Roles = "User")] // Только авторизованные пользователи могут обновлять оценки
        public async Task<IActionResult> PutRatingForCardDetail(int cardDetailId, [FromBody] RatingDto ratingDto)
        {
            // Ищем существующую запись оценок для этой CardDetail
            var rating = await _context.Ratings
                                       .FirstOrDefaultAsync(r => r.CardDetailId == cardDetailId);

            if (rating == null)
            {
                return NotFound($"Оценки для CardDetail с ID {cardDetailId} не найдены. Создайте CardDetail сначала.");
            }

            // Обновляем значения оценок
            rating.Cleanliness = ratingDto.Cleanliness;
            rating.Accuracy = ratingDto.Accuracy;
            rating.CheckIn = ratingDto.CheckIn;
            rating.Communication = ratingDto.Communication;
            rating.Location = ratingDto.Location;
            rating.Value = ratingDto.Value;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // В данном случае DbUpdateConcurrencyException маловероятен для PUT по FK,
                // но оставляем на всякий случай или для более сложных сценариев.
                if (!RatingExists(rating.Id)) // Проверяем существование записи по ее собственному ID
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при обновлении оценок для CardDetailId {cardDetailId}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при обновлении оценок.");
            }

            return NoContent();
        }

        // Вспомогательный метод для проверки существования Rating по его собственному ID
        private bool RatingExists(int id)
        {
            return _context.Ratings.Any(e => e.Id == id);
        }
    }
}
