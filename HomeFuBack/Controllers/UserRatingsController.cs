// HomeFuBack.Controllers/UserRatingsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing; // Для UserRating, CardDetail
using HomeFuBack.Models.Users; // Для User
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/carddetails/{cardDetailId}/ratings")] // Маршрут для оценок к конкретной CardDetail
    // [Authorize] // Если требуется авторизация для оценки
    public class UserRatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserRatingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/carddetails/{cardDetailId}/ratings
        // Получить все пользовательские оценки для конкретной детальной карточки
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRatingResponseDto>>> GetUserRatingsForCardDetail(int cardDetailId)
        {
            var userRatings = await _context.UserRatings
                .Where(ur => ur.CardDetailId == cardDetailId)
                .Include(ur => ur.User) // Загружаем информацию о пользователе, оставившем оценку
                .OrderByDescending(ur => ur.CreatedAt) // Сортируем по дате создания, новейшие сверху
                .ToListAsync();

            if (!userRatings.Any())
            {
                return Ok(new List<UserRatingResponseDto>());
            }

            var response = userRatings.Select(MapUserRatingToResponseDto).ToList();
            return Ok(response);
        }

        // GET: api/carddetails/{cardDetailId}/ratings/{ratingId}
        // Получить конкретную пользовательскую оценку по ID
        [HttpGet("{ratingId}")]
        public async Task<ActionResult<UserRatingResponseDto>> GetUserRating(int cardDetailId, int ratingId)
        {
            var userRating = await _context.UserRatings
                .Where(ur => ur.CardDetailId == cardDetailId && ur.Id == ratingId)
                .Include(ur => ur.User)
                .FirstOrDefaultAsync();

            if (userRating == null)
            {
                return NotFound($"Оценка с ID {ratingId} для детальной карточки {cardDetailId} не найдена.");
            }

            return Ok(MapUserRatingToResponseDto(userRating));
        }


        // POST: api/carddetails/{cardDetailId}/ratings
        // Создать новую пользовательскую оценку для детальной карточки
        [HttpPost]
        public async Task<ActionResult<UserRatingResponseDto>> CreateUserRating(int cardDetailId, [FromBody] UserRatingCreateDto dto)
        {
            // 1. Проверяем существование CardDetail
            var cardDetail = await _context.CardDetails.FindAsync(cardDetailId);
            if (cardDetail == null)
            {
                return NotFound($"Детальная карточка с ID {cardDetailId} не найдена.");
            }

            // 2. Проверяем существование пользователя
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return BadRequest($"Пользователь с ID {dto.UserId} не найден.");
            }

            // 3. Проверяем, оставлял ли пользователь уже оценку для этой карточки (если установлен UniqueIndex)
            var existingRating = await _context.UserRatings
                .AnyAsync(ur => ur.CardDetailId == cardDetailId && ur.UserId == dto.UserId);
            if (existingRating)
            {
                return Conflict($"Пользователь с ID {dto.UserId} уже оставил оценку для этой карточки.");
            }

            // 4. Создаем новую оценку
            var userRating = new UserRating
            {
                CardDetailId = cardDetailId,
                UserId = dto.UserId,
                Cleanliness = dto.Cleanliness,
                Accuracy = dto.Accuracy,
                CheckIn = dto.CheckIn,
                Communication = dto.Communication,
                Location = dto.Location,
                Value = dto.Value,
                CreatedAt = DateTime.UtcNow // Устанавливаем время создания на сервере
            };

            _context.UserRatings.Add(userRating);

            try
            {
                await _context.SaveChangesAsync();

                // Загружаем пользователя для DTO ответа, если он еще не загружен
                await _context.Entry(userRating).Reference(ur => ur.User).LoadAsync();

                // ОПЦИОНАЛЬНО: Обновление агрегированной оценки в CardDetail.Ratings
                // Это может быть сделано здесь или в отдельном фоновом процессе/триггере БД.
                // Для простоты, сделаем это здесь.
                await UpdateCardDetailAggregateRating(cardDetailId);


                return CreatedAtAction(
                    nameof(GetUserRating),
                    new { cardDetailId = userRating.CardDetailId, ratingId = userRating.Id },
                    MapUserRatingToResponseDto(userRating));
            }
            catch (Exception ex)
            {
                // Если ошибка связана с уникальным индексом (хотя мы уже проверили), поймать ее
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 2601) // 2601 для UniqueIndex Violation in SQL Server
                {
                    return Conflict($"Ошибка: Пользователь с ID {dto.UserId} уже оставил оценку для этой карточки.");
                }
                Console.WriteLine($"Ошибка при создании пользовательской оценки: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при создании оценки.");
            }
        }

        // PUT: api/carddetails/{cardDetailId}/ratings/{ratingId}
        // Обновить существующую пользовательскую оценку
        [HttpPut("{ratingId}")]
        public async Task<IActionResult> UpdateUserRating(int cardDetailId, int ratingId, [FromBody] UserRatingCreateDto dto)
        {
            var userRating = await _context.UserRatings
                .Where(ur => ur.CardDetailId == cardDetailId && ur.Id == ratingId)
                .FirstOrDefaultAsync();

            if (userRating == null)
            {
                return NotFound($"Оценка с ID {ratingId} для детальной карточки {cardDetailId} не найдена.");
            }

            // ОПЦИОНАЛЬНО: Добавить логику проверки авторизации здесь:
            // if (userRating.UserId != User.GetUserId()) { return Forbid(); }

            // Обновляем поля
            if (dto.Cleanliness.HasValue) userRating.Cleanliness = dto.Cleanliness.Value;
            if (dto.Accuracy.HasValue) userRating.Accuracy = dto.Accuracy.Value;
            if (dto.CheckIn.HasValue) userRating.CheckIn = dto.CheckIn.Value;
            if (dto.Communication.HasValue) userRating.Communication = dto.Communication.Value;
            if (dto.Location.HasValue) userRating.Location = dto.Location.Value;
            if (dto.Value.HasValue) userRating.Value = dto.Value.Value;

            _context.Entry(userRating).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // ОПЦИОНАЛЬНО: Обновление агрегированной оценки в CardDetail.Ratings
                await UpdateCardDetailAggregateRating(cardDetailId);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.UserRatings.AnyAsync(ur => ur.Id == ratingId))
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
                Console.WriteLine($"Ошибка при обновлении пользовательской оценки {ratingId}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при обновлении оценки.");
            }
        }

        // DELETE: api/carddetails/{cardDetailId}/ratings/{ratingId}
        // Удалить пользовательскую оценку
        [HttpDelete("{ratingId}")]
        public async Task<IActionResult> DeleteUserRating(int cardDetailId, int ratingId)
        {
            var userRating = await _context.UserRatings
                .Where(ur => ur.CardDetailId == cardDetailId && ur.Id == ratingId)
                .FirstOrDefaultAsync();

            if (userRating == null)
            {
                return NotFound($"Оценка с ID {ratingId} для детальной карточки {cardDetailId} не найдена.");
            }

            // ОПЦИОНАЛЬНО: Добавить логику проверки авторизации здесь:
            // if (userRating.UserId != User.GetUserId()) { return Forbid(); }

            _context.UserRatings.Remove(userRating);

            try
            {
                await _context.SaveChangesAsync();

                // ОПЦИОНАЛЬНО: Обновление агрегированной оценки в CardDetail.Ratings
                await UpdateCardDetailAggregateRating(cardDetailId);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении пользовательской оценки {ratingId}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при удалении оценки.");
            }
        }

        // --- Вспомогательные методы ---

        // Метод для маппинга UserRating в UserRatingResponseDto
        private UserRatingResponseDto MapUserRatingToResponseDto(UserRating userRating)
        {
            return new UserRatingResponseDto
            {
                Id = userRating.Id,
                CreatedAt = userRating.CreatedAt,
                CardDetailId = userRating.CardDetailId,
                UserId = userRating.UserId,
                UserName = userRating.User?.FirstName ?? "Unknown User",
                UserProfileImageUrl = userRating.User?.ProfileImageUrl,
                Cleanliness = userRating.Cleanliness,
                Accuracy = userRating.Accuracy,
                CheckIn = userRating.CheckIn,
                Communication = userRating.Communication,
                Location = userRating.Location,
                Value = userRating.Value,
                OverallRating = userRating.OverallRating
            };
        }

        // ОПЦИОНАЛЬНО: Метод для пересчета и обновления агрегированной оценки в CardDetail.Ratings
        private async Task UpdateCardDetailAggregateRating(int cardDetailId)
        {
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.UserRatings) // Загружаем все UserRatings для пересчета
                .Include(cd => cd.Ratings) // Загружаем существующую агрегированную оценку
                .FirstOrDefaultAsync(cd => cd.Id == cardDetailId);

            if (cardDetail == null) return; // Или бросить исключение/залогировать

            var allUserRatings = cardDetail.UserRatings.ToList();

            if (!allUserRatings.Any())
            {
                // Если оценок нет, можно обнулить агрегированную оценку
                if (cardDetail.Ratings != null)
                {
                    _context.Ratings.Remove(cardDetail.Ratings);
                    cardDetail.Ratings = null;
                }
            }
            else
            {
                var avgCleanliness = allUserRatings.Where(r => r.Cleanliness.HasValue).Average(r => r.Cleanliness.Value);
                var avgAccuracy = allUserRatings.Where(r => r.Accuracy.HasValue).Average(r => r.Accuracy.Value);
                var avgCheckIn = allUserRatings.Where(r => r.CheckIn.HasValue).Average(r => r.CheckIn.Value);
                var avgCommunication = allUserRatings.Where(r => r.Communication.HasValue).Average(r => r.Communication.Value);
                var avgLocation = allUserRatings.Where(r => r.Location.HasValue).Average(r => r.Location.Value);
                var avgValue = allUserRatings.Where(r => r.Value.HasValue).Average(r => r.Value.Value);

                if (cardDetail.Ratings == null)
                {
                    cardDetail.Ratings = new Rating
                    {
                        CardDetailId = cardDetailId,
                        Cleanliness = avgCleanliness,
                        Accuracy = avgAccuracy,
                        CheckIn = avgCheckIn,
                        Communication = avgCommunication,
                        Location = avgLocation,
                        Value = avgValue
                    };
                    _context.Ratings.Add(cardDetail.Ratings);
                }
                else
                {
                    cardDetail.Ratings.Cleanliness = avgCleanliness;
                    cardDetail.Ratings.Accuracy = avgAccuracy;
                    cardDetail.Ratings.CheckIn = avgCheckIn;
                    cardDetail.Ratings.Communication = avgCommunication;
                    cardDetail.Ratings.Location = avgLocation;
                    cardDetail.Ratings.Value = avgValue;
                    _context.Entry(cardDetail.Ratings).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync(); // Сохраняем изменения в агрегированной оценке
        }
    }
}