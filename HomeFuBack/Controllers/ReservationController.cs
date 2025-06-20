using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing;
using HomeFuBack.Models.Users;
using Microsoft.AspNetCore.Authorization; // Для авторизации
using HomeFuBack.Helpers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Для получения ID пользователя из Claims

namespace HomeFuBack.Controllers
{
    [Route("api/reservation")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public ReservationController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

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
                UserName = reservation.User?.FirstName,
                UserEmail = reservation.User?.Email,
                CreatedAt = reservation.CreatedAt,
                Status = reservation.Status.ToString() // Преобразуем enum в string
            };
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetAllReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Card)
                .Include(r => r.User) 
                .ToListAsync();

            var reservationDtos = reservations.Select(r => MapToReservationResponseDto(r)).ToList();
            return Ok(reservationDtos);
        }

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


        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

            // Получение данных о жилье, включая детали и информацию о владельце (хосте)
            var card = await _context.Cards
                                     .Include(c => c.CardDetail)
                                         .ThenInclude(cd => cd.Host) // Загружаем Host из CardDetail
                                     .FirstOrDefaultAsync(c => c.Id == reservationDto.CardId);

            if (card == null)
            {
                return NotFound("Жилье не найдено.");
            }

            if (card.CardDetail == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Детальная информация о жилье недоступна.");
            }

            if (card.CardDetail.Host == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Не удалось определить владельца жилья.");
            }

            var totalGuests = reservationDto.Adults + reservationDto.Children;
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
                Status = ReservationStatus.Confirmed
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            
            await _context.Entry(reservation).Reference(r => r.User).LoadAsync(); // Данных клиента для использования в письме

            try
            {
                var client = reservation.User;
                var host = card.CardDetail.Host;

                var numberOfNights = (reservation.CheckOutDate - reservation.CheckInDate).Days;
                var totalPrice = numberOfNights * card.Price;

                //Письмо для клиента
                var clientSubject = $"Ваша резервация в {card.Name} подтверждена!";
                var clientMessage = $@"
            <h1>Резервация успешна!</h1>
            <p>Здравствуйте, {client!.FirstName},</p>
            <p>Ваша резервация для жилья ""{card.Name}"" была успешно создана и подтверждена.</p>
            <h3>Детали вашей поездки:</h3>
            <ul>
                <li><strong>Дата заезда:</strong> {reservation.CheckInDate:dd MMMM yyyy}</li>
                <li><strong>Дата выезда:</strong> {reservation.CheckOutDate:dd MMMM yyyy}</li>
                <li><strong>Количество ночей:</strong> {numberOfNights}</li>
                <li><strong>Гости:</strong> {reservation.Adults} взрослых, {reservation.Children} детей, {reservation.Infants} младенцев</li>
                <li><strong>Питомцы:</strong> {reservation.Pets}</li>
                <li><strong>Итоговая сумма:</strong> {totalPrice:C}</li>
            </ul>
            <p>Спасибо, что выбрали HomeFu!</p>";

                await _emailSender.SendEmailAsync(client.Email, clientSubject, clientMessage); // Данных хоста для использования в письме

                // Письмо для хоста
                if (host.Email != client.Email)
                {
                    var hostSubject = $"Новая резервация вашего жилья: {card.Name}";
                    var hostMessage = $@"
                <h1>У вас новая резервация!</h1>
                <p>Здравствуйте, {host.FirstName},</p>
                <p>Ваше жилье ""{card.Name}"" было зарезервировано.</p>
                <h3>Детали резервации:</h3>
                <ul>
                    <li><strong>Имя гостя:</strong> {client.FirstName} {client.LastName}</li>
                    <li><strong>Email гостя:</strong> {client.Email}</li>
                    <li><strong>Дата заезда:</strong> {reservation.CheckInDate:dd MMMM yyyy}</li>
                    <li><strong>Дата выезда:</strong> {reservation.CheckOutDate:dd MMMM yyyy}</li>
                    <li><strong>Гости:</strong> {reservation.Adults} взрослых, {reservation.Children} детей, {reservation.Infants} младенцев</li>
                    <li><strong>Питомцы:</strong> {reservation.Pets}</li>
                    <li><strong>Итоговая сумма:</strong> {totalPrice:C}</li>
                </ul>
                <p>Вы можете управлять резервациями в вашей панели управления HomeFu.</p>";

                    await _emailSender.SendEmailAsync(host.Email, hostSubject, hostMessage);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем основной процесс.
                Console.WriteLine($"[WARNING] Ошибка при отправке email-уведомлений для резервации ID:{reservation.Id}. Ошибка: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, MapToReservationResponseDto(reservation));
        }


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

            if (reservation == null) {return NotFound($"Резервация с ID {id} не найдена.");}

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Admin") && (string.IsNullOrEmpty(userIdClaim) 
                                            || !Guid.TryParse(userIdClaim, out Guid userIdGuid) 
                                            || reservation.UserId != userIdGuid))
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