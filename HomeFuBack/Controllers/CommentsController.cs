using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/carddetails/{cardDetailId}/comments")] // Маршрут для комментариев к конкретной CardDetail
    // [Authorize] // Если требуется авторизация для комментирования
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/carddetails/{cardDetailId}/comments
        // Получить все комментарии для конкретной детальной карточки
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetCommentsForCardDetail(int cardDetailId)
        {
            var comments = await _context.Comments
                .Where(c => c.CardDetailId == cardDetailId)
                .Include(c => c.User) // Загружаем информацию о пользователе, оставившем комментарий
                .OrderByDescending(c => c.CreatedAt) // Сортируем по дате создания, новейшие сверху
                .ToListAsync();

            if (!comments.Any())
            {
                // Можно вернуть Ok с пустым списком или NotFound, если нет комментариев.
                // Возврат пустого списка предпочтительнее.
                return Ok(new List<CommentResponseDto>());
            }

            var response = comments.Select(MapCommentToResponseDto).ToList();
            return Ok(response);
        }

        // POST: api/carddetails/{cardDetailId}/comments
        // Создать новый комментарий для детальной карточки
        [HttpPost]
        public async Task<ActionResult<CommentResponseDto>> CreateComment(int cardDetailId, [FromBody] CommentCreateDto dto)
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

            // 3. Создаем новый комментарий
            var comment = new Comment
            {
                Text = dto.Text,
                CardDetailId = cardDetailId,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow // Устанавливаем время создания на сервере
            };

            _context.Comments.Add(comment);

            try
            {
                await _context.SaveChangesAsync();

                // Загружаем пользователя для DTO ответа, если он еще не загружен
                await _context.Entry(comment).Reference(c => c.User).LoadAsync();

                return CreatedAtAction(
                    nameof(GetCommentsForCardDetail), // Используем имя метода GET для этого маршрута
                    new { cardDetailId = comment.CardDetailId },
                    MapCommentToResponseDto(comment));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании комментария: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при создании комментария.");
            }
        }

        // DELETE: api/carddetails/{cardDetailId}/comments/{id}
        // Удалить комментарий (возможно, только для автора или администратора)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int cardDetailId, int id)
        {
            var comment = await _context.Comments
                .Where(c => c.CardDetailId == cardDetailId && c.Id == id)
                .FirstOrDefaultAsync();

            if (comment == null)
            {
                return NotFound($"Комментарий с ID {id} для детальной карточки {cardDetailId} не найден.");
            }

            // Добавить логику проверки авторизации здесь:
            // Например: if (comment.UserId != User.GetUserId()) { return Forbid(); }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
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
                UserName = comment.User?.FirstName ?? "Unknown User", // Используем null-conditional operator
                UserProfileImageUrl = comment.User?.ProfileImageUrl
            };
        }
    }
}