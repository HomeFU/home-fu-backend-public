// HomeFuBack.Controllers/CommentsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing; // Для Comment, CardDetail, Rating
using HomeFuBack.Models.Users; // Для User
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeFuBack.Helpers;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/carddetails/{cardDetailId}/reviews")] // Переименуем маршрут для ясности (было /comments)
    // [Authorize]
    public class CommentsController : ControllerBase // Можно переименовать в ReviewsController
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/carddetails/{cardDetailId}/reviews
        // Получить все отзывы (комментарии с оценками) для конкретной детальной карточки
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetReviewsForCardDetail(int cardDetailId)
        {
            var reviews = await _context.Comments // Теперь Comments содержит и отзывы
                .Where(c => c.CardDetailId == cardDetailId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (!reviews.Any())
            {
                return Ok(new List<CommentResponseDto>());
            }

            var response = reviews.Select(MapCommentToResponseDto).ToList();
            return Ok(response);
        }

        // POST: api/carddetails/{cardDetailId}/reviews
        [HttpPost]
        public async Task<ActionResult<CommentResponseDto>> CreateReview(int cardDetailId, [FromBody] ReviewCreateDto dto)
        {
            // Получаем UserId из токена авторизованного пользователя
            var userId = User.GetUserId(); // Используем наш вспомогательный метод

            // 1. Проверяем существование CardDetail
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Ratings)
                .FirstOrDefaultAsync(cd => cd.Id == cardDetailId);
            if (cardDetail == null)
            {
                return NotFound($"Детальная карточка с ID {cardDetailId} не найдена.");
            }

            // 2. Проверяем существование пользователя (хотя если токен валидный, пользователь должен существовать)
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                // Это маловероятно, если ваша система авторизации настроена корректно
                return Unauthorized("Пользователь, связанный с токеном, не найден.");
            }

            // 3. (ОПЦИОНАЛЬНО) Проверяем, оставлял ли пользователь уже отзыв для этой карточки
            var existingReview = await _context.Comments
                .AnyAsync(c => c.CardDetailId == cardDetailId && c.UserId == userId); // Используем userId из токена
            if (existingReview)
            {
                return Conflict($"Вы уже оставили отзыв для этой карточки.");
            }

            // 4. Создаем новый Comment/Review
            var review = new Comment
            {
                Text = dto.Text,
                CardDetailId = cardDetailId,
                UserId = userId, // Используем userId из токена
                Cleanliness = dto.Cleanliness,
                Accuracy = dto.Accuracy,
                CheckIn = dto.CheckIn,
                Communication = dto.Communication,
                Location = dto.Location,
                Value = dto.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(review);

            try
            {
                await _context.SaveChangesAsync();

                await _context.Entry(review).Reference(c => c.User).LoadAsync();

                await UpdateAggregateRatings(cardDetailId);

                return CreatedAtAction(
                    nameof(GetReviewsForCardDetail),
                    new { cardDetailId = review.CardDetailId },
                    MapCommentToResponseDto(review));
            }
            catch (Exception ex)
            {
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 2601)
                {
                    return Conflict($"Ошибка: Вы уже оставили отзыв для этой карточки.");
                }
                Console.WriteLine($"Ошибка при создании отзыва: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при создании отзыва.");
            }
        }

        // PUT: api/carddetails/{cardDetailId}/reviews/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int cardDetailId, int id, [FromBody] ReviewCreateDto dto)
        {
            var userId = User.GetUserId(); // Получаем ID текущего пользователя

            var review = await _context.Comments
                .Where(c => c.CardDetailId == cardDetailId && c.Id == id)
                .FirstOrDefaultAsync();

            if (review == null)
            {
                return NotFound($"Отзыв с ID {id} для детальной карточки {cardDetailId} не найден.");
            }

            // Проверка, что только автор может обновить отзыв
            if (review.UserId != userId)
            {
                return Forbid("У вас нет прав для обновления этого отзыва.");
            }

            // Обновляем поля
            review.Text = dto.Text;
            if (dto.Cleanliness.HasValue) review.Cleanliness = dto.Cleanliness.Value;
            if (dto.Accuracy.HasValue) review.Accuracy = dto.Accuracy.Value;
            if (dto.CheckIn.HasValue) review.CheckIn = dto.CheckIn.Value;
            if (dto.Communication.HasValue) review.Communication = dto.Communication.Value;
            if (dto.Location.HasValue) review.Location = dto.Location.Value;
            if (dto.Value.HasValue) review.Value = dto.Value.Value;

            _context.Entry(review).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await UpdateAggregateRatings(cardDetailId);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Comments.AnyAsync(c => c.Id == id))
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
                Console.WriteLine($"Ошибка при обновлении отзыва {id}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при обновлении отзыва.");
            }
        }

        // DELETE: api/carddetails/{cardDetailId}/reviews/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int cardDetailId, int id)
        {
            var userId = User.GetUserId(); // Получаем ID текущего пользователя

            var review = await _context.Comments
                .Where(c => c.CardDetailId == cardDetailId && c.Id == id)
                .FirstOrDefaultAsync();

            if (review == null)
            {
                return NotFound($"Отзыв с ID {id} для детальной карточки {cardDetailId} не найден.");
            }

            // Проверка, что только автор или администратор может удалить отзыв
            if (review.UserId != userId) // && !User.IsInRole("Admin")
            {
                return Forbid("У вас нет прав для удаления этого отзыва.");
            }

            _context.Comments.Remove(review);

            try
            {
                await _context.SaveChangesAsync();
                await UpdateAggregateRatings(cardDetailId);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении отзыва {id}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при удалении.");
            }
        }

        // Вспомогательный метод для маппинга Comment в CommentResponseDto
        private CommentResponseDto MapCommentToResponseDto(Comment comment)
        {
            return new CommentResponseDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                CardDetailId = comment.CardDetailId,
                UserId = comment.UserId,
                UserName = comment.User?.FirstName ?? "Unknown User",
                UserProfileImageUrl = comment.User?.ProfileImageUrl,
                Cleanliness = comment.Cleanliness,
                Accuracy = comment.Accuracy,
                CheckIn = comment.CheckIn,
                Communication = comment.Communication,
                Location = comment.Location,
                Value = comment.Value,
                OverallRating = comment.OverallRating // Используем вычисляемое свойство
            };
        }

        // НОВЫЙ/ОБНОВЛЕННЫЙ метод для пересчета и обновления агрегированных оценок
        private async Task UpdateAggregateRatings(int cardDetailId)
        {
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Comments) // Теперь агрегируем по Comments
                .Include(cd => cd.Ratings) // Для CardDetail.Ratings
                .Include(cd => cd.Card) // Для Card.Rating
                .FirstOrDefaultAsync(cd => cd.Id == cardDetailId);

            if (cardDetail == null) return;

            var reviewsWithRatings = cardDetail.Comments
                .Where(c => c.OverallRating.HasValue) // Только отзывы, где есть оценки
                .ToList();

            double? newOverallCardRating = null; // Для Card.Rating
            Rating currentDetailRating = cardDetail.Ratings; // Для CardDetail.Ratings

            if (reviewsWithRatings.Any())
            {
                var avgCleanliness = reviewsWithRatings.Average(r => r.Cleanliness ?? 0); // Используем ?? 0 для nullable
                var avgAccuracy = reviewsWithRatings.Average(r => r.Accuracy ?? 0);
                var avgCheckIn = reviewsWithRatings.Average(r => r.CheckIn ?? 0);
                var avgCommunication = reviewsWithRatings.Average(r => r.Communication ?? 0);
                var avgLocation = reviewsWithRatings.Average(r => r.Location ?? 0);
                var avgValue = reviewsWithRatings.Average(r => r.Value ?? 0);

                newOverallCardRating = reviewsWithRatings.Average(r => r.OverallRating!.Value); // !.Value т.к. мы уже отфильтровали HasValue

                // Обновляем/создаем агрегированную оценку в CardDetail.Ratings
                if (currentDetailRating == null)
                {
                    currentDetailRating = new Rating
                    {
                        CardDetailId = cardDetailId,
                        Cleanliness = avgCleanliness,
                        Accuracy = avgAccuracy,
                        CheckIn = avgCheckIn,
                        Communication = avgCommunication,
                        Location = avgLocation,
                        Value = avgValue
                    };
                    _context.Ratings.Add(currentDetailRating);
                }
                else
                {
                    currentDetailRating.Cleanliness = avgCleanliness;
                    currentDetailRating.Accuracy = avgAccuracy;
                    currentDetailRating.CheckIn = avgCheckIn;
                    currentDetailRating.Communication = avgCommunication;
                    currentDetailRating.Location = avgLocation;
                    currentDetailRating.Value = avgValue;
                    _context.Entry(currentDetailRating).State = EntityState.Modified;
                }
            }
            else // Если оценок нет
            {
                // Если агрегированная оценка существует, удаляем её
                if (currentDetailRating != null)
                {
                    _context.Ratings.Remove(currentDetailRating);
                    cardDetail.Ratings = null; // Отвязываем от CardDetail
                }
                newOverallCardRating = null; // Сбрасываем общую оценку карточки
            }

            // Обновляем агрегированную оценку в Card.Rating
            if (cardDetail.Card != null)
            {
                cardDetail.Card.Rating = Convert.ToInt32(newOverallCardRating);
                _context.Entry(cardDetail.Card).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }
    }
}