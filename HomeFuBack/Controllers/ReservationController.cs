using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data; // Ваш ApplicationDbContext
using HomeFuBack.Models.Housing; // Ваша модель Reservation, Card, ReservationStatus
using HomeFuBack.Models.Users; // Ваша модель User (если используется навигационное свойство User)
using HomeFuBack.Data.DTO; // Ваши DTO (ReservationDto, ReservationUpdateDto, ReservationResponseDto)
using System.Security.Claims; // Для получения ID пользователя из Claims
using Microsoft.AspNetCore.Authorization; // Для авторизации

namespace HomeFuBack.Controllers
{
    [Route("api/reservation")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Вспомогательные методы для ручного маппинга ---

        // Маппинг из Reservation в ReservationResponseDto
        private ReservationResponseDto MapToReservationResponseDto(Reservation reservation)
        {
            return new ReservationResponseDto
            {
                Id = reservation.Id,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                Adults = reservation.Adults,
                Children = reservation.Children,
                Infants = reservation.Infants,
                Pets = reservation.Pets,
                CardId = reservation.CardId,
                CardName = reservation.Card?.Name, // Используем оператор ? для Null-Conditional
                CardImageUrls = reservation.Card?.ImageUrls ?? new List<string>(), // Null-Coalescing для List<string>
                UserId = reservation.UserId,
                UserName = reservation.User?.FirstName, // Предполагается, что у User есть UserName
                UserEmail = reservation.User?.Email,   // Предполагается, что у User есть Email
                CreatedAt = reservation.CreatedAt,
                Status = reservation.Status.ToString() // Преобразуем enum в string
            };
        }

        // --- GET Endpoints ---

        /// <summary>
        /// Получает список всех резерваций. Только для администраторов.
        /// </summary>
        /// <returns>Список ReservationResponseDto.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetAllReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Card)
                .Include(r => r.User) // Предполагается, что у вас есть DbSet<User> и навигационное свойство
                .ToListAsync();

            var reservationDtos = reservations.Select(r => MapToReservationResponseDto(r)).ToList();
            return Ok(reservationDtos);
        }

        /// <summary>
        /// Получает резервацию по ID. Только владелец или администратор.
        /// </summary>
        /// <param name="id">ID резервации.</param>
        /// <returns>ReservationResponseDto.</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ReservationResponseDto>> GetReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Card)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound($"Резервация с ID {id} не найдена.");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && reservation.UserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            return Ok(MapToReservationResponseDto(reservation));
        }

        /// <summary>
        /// Получает все резервации для текущего авторизованного пользователя.
        /// </summary>
        /// <returns>Список ReservationResponseDto.</returns>
        [HttpGet("user")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetUserReservations()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Невозможно определить ID пользователя из токена.");
            }

            var reservations = await _context.Reservations
                .Where(r => r.UserId == userId)
                .Include(r => r.Card)
                .Include(r => r.User)
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound($"У пользователя с ID {userId} нет активных резерваций.");
            }

            var reservationDtos = reservations.Select(r => MapToReservationResponseDto(r)).ToList();
            return Ok(reservationDtos);
        }

        // --- POST Endpoints ---

        /// <summary>
        /// Создает новую резервацию для текущего пользователя.
        /// </summary>
        /// <param name="reservationDto">Данные для создания резервации.</param>
        /// <returns>Созданная ReservationResponseDto.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReservationResponseDto>> PostReservation([FromBody] ReservationDto reservationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userIdGuid))
            {
                return Unauthorized("Невозможно определить ID пользователя из токена или некорректный формат.");
            }

            if (reservationDto.CheckInDate >= reservationDto.CheckOutDate)
            {
                ModelState.AddModelError(nameof(reservationDto.CheckOutDate), "Дата выезда должна быть после даты заезда.");
                return BadRequest(ModelState);
            }

            if (reservationDto.CheckInDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(reservationDto.CheckInDate), "Дата заезда не может быть в прошлом.");
                return BadRequest(ModelState);
            }

            var card = await _context.Cards
                                     .Include(c => c.CardDetail)
                                     .FirstOrDefaultAsync(c => c.Id == reservationDto.CardId);
            if (card == null)
            {
                return NotFound("Жилье не найдено.");
            }

            if (card.CardDetail == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Детальная информация о жилье недоступна.");
            }

            var totalGuests = reservationDto.Adults + reservationDto.Children + reservationDto.Infants;
            if (totalGuests > card.CardDetail.NumberOfGuests)
            {
                ModelState.AddModelError(nameof(reservationDto.Adults), $"Общее количество гостей ({totalGuests}) превышает максимально допустимое для этого жилья ({card.CardDetail.NumberOfGuests}).");
                return BadRequest(ModelState);
            }

            var hasOverlap = await _context.Reservations
                .AnyAsync(r => r.CardId == reservationDto.CardId &&
                               r.Status != ReservationStatus.Cancelled &&
                               r.Status != ReservationStatus.Completed &&
                               ((reservationDto.CheckInDate < r.CheckOutDate) && (reservationDto.CheckOutDate > r.CheckInDate)));

            if (hasOverlap)
            {
                return Conflict("Выбранные даты недоступны для данного жилья. Есть пересечения с существующими резервациями.");
            }

            var reservation = new Reservation
            {
                CheckInDate = reservationDto.CheckInDate,
                CheckOutDate = reservationDto.CheckOutDate,
                Adults = reservationDto.Adults,
                Children = reservationDto.Children,
                Infants = reservationDto.Infants,
                Pets = reservationDto.Pets,
                CardId = reservationDto.CardId,
                UserId = userIdGuid,
                CreatedAt = DateTime.UtcNow,
                Status = ReservationStatus.Pending
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Загрузка связанных данных для корректного маппинга в ResponseDto
            // Эти .LoadAsync() нужны, только если вы хотите, чтобы MapToReservationResponseDto
            // имел доступ к Card.Name, Card.ImageUrls, User.UserName и User.Email сразу после сохранения
            await _context.Entry(reservation).Reference(r => r.Card).LoadAsync();
            await _context.Entry(reservation).Reference(r => r.User).LoadAsync();

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, MapToReservationResponseDto(reservation));
        }

        // --- PUT Endpoints ---

        /// <summary>
        /// Обновляет существующую резервацию. Только владелец или администратор.
        /// </summary>
        /// <param name="id">ID резервации для обновления.</param>
        /// <param name="reservationUpdateDto">Данные для обновления резервации.</param>
        /// <returns>NoContent.</returns>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PutReservation(int id, [FromBody] ReservationUpdateDto reservationUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userIdGuid))
            {
                return Unauthorized("Невозможно определить ID пользователя из токена или некорректный формат.");
            }

            var reservation = await _context.Reservations
                                            .Include(r => r.Card)
                                                .ThenInclude(c => c.CardDetail)
                                            .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound($"Резервация с ID {id} не найдена.");
            }

            if (!User.IsInRole("Admin") && reservation.UserId != userIdGuid)
            {
                return Forbid("Вы можете изменять только свои резервации.");
            }

            // Ручное обновление полей из DTO
            if (reservationUpdateDto.CheckInDate.HasValue)
                reservation.CheckInDate = reservationUpdateDto.CheckInDate.Value;

            if (reservationUpdateDto.CheckOutDate.HasValue)
                reservation.CheckOutDate = reservationUpdateDto.CheckOutDate.Value;

            if (reservationUpdateDto.Adults.HasValue)
                reservation.Adults = reservationUpdateDto.Adults.Value;

            if (reservationUpdateDto.Children.HasValue)
                reservation.Children = reservationUpdateDto.Children.Value;

            if (reservationUpdateDto.Infants.HasValue)
                reservation.Infants = reservationUpdateDto.Infants.Value;

            if (reservationUpdateDto.Pets.HasValue)
                reservation.Pets = reservationUpdateDto.Pets.Value;

            if (reservationUpdateDto.Status.HasValue)
                reservation.Status = reservationUpdateDto.Status.Value;
            // Если Status в DTO был string, здесь бы потребовался Enum.TryParse

            // Валидация дат после обновления
            if (reservation.CheckInDate >= reservation.CheckOutDate)
            {
                ModelState.AddModelError(nameof(reservation.CheckOutDate), "Дата выезда должна быть после даты заезда.");
                return BadRequest(ModelState);
            }

            // Проверка вместимости после обновления
            if (reservation.Card?.CardDetail == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Детальная информация о жилье недоступна для проверки вместимости.");
            }
            var totalGuests = reservation.Adults + reservation.Children + reservation.Infants;
            if (totalGuests > reservation.Card.CardDetail.NumberOfGuests)
            {
                ModelState.AddModelError(nameof(reservationUpdateDto.Adults), $"Общее количество гостей ({totalGuests}) превышает максимально допустимое для этого жилья ({reservation.Card.CardDetail.NumberOfGuests}).");
                return BadRequest(ModelState);
            }

            // Проверка на конфликты дат
            var hasOverlap = await _context.Reservations
                .AnyAsync(r => r.Id != id &&
                               r.CardId == reservation.CardId &&
                               r.Status != ReservationStatus.Cancelled &&
                               r.Status != ReservationStatus.Completed &&
                               ((reservation.CheckInDate < r.CheckOutDate) && (reservation.CheckOutDate > r.CheckInDate)));

            if (hasOverlap)
            {
                return Conflict("Обновленные даты недоступны для данного жилья. Есть пересечения с существующими резервациями.");
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
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

        /// <summary>
        /// Отменяет резервацию (изменяет статус на "Cancelled"). Только владелец или администратор.
        /// </summary>
        /// <param name="id">ID резервации для отмены.</param>
        /// <returns>NoContent.</returns>
        [HttpPost("{id}/cancel")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound($"Резервация с ID {id} не найдена.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Admin") && (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userIdGuid) || reservation.UserId != userIdGuid))
            {
                return Forbid("Вы можете отменять только свои резервации.");
            }

            if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Completed)
            {
                return BadRequest("Невозможно отменить резервацию с текущим статусом.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Удаляет резервацию. Только для администраторов. (Физическое удаление)
        /// </summary>
        /// <param name="id">ID резервации для удаления.</param>
        /// <returns>NoContent.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound($"Резервация с ID {id} не найдена.");
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/reservation/card/{cardId}/availability
        [HttpGet("card/{cardId}/availability")]
        public async Task<ActionResult<IEnumerable<object>>> GetCardAvailability(int cardId, DateTime? from = null, DateTime? to = null)
        {
            var cardExists = await _context.Cards.AnyAsync(c => c.Id == cardId);
            if (!cardExists)
            {
                return NotFound($"Карточка объявления с ID {cardId} не найдена.");
            }

            var fromDate = from?.Date ?? DateTime.Now.Date;
            var toDate = to?.Date ?? DateTime.Now.Date.AddMonths(6);

            if (fromDate > toDate)
            {
                return BadRequest("Дата 'from' не может быть позже даты 'to'.");
            }

            var bookedDates = await _context.Reservations
                .Where(r => r.CardId == cardId &&
                            r.Status != ReservationStatus.Cancelled &&
                            r.Status != ReservationStatus.Completed &&
                            r.CheckOutDate > fromDate &&
                            r.CheckInDate < toDate)
                .Select(r => new
                {
                    CheckIn = r.CheckInDate.Date,
                    CheckOut = r.CheckOutDate.Date
                })
                .ToListAsync();

            return Ok(bookedDates);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}