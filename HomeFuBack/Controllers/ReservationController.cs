using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/reservation")]
    public class ReservationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reservation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations()
        {
            return await _context.Reservations.Include(r => r.Card).ToListAsync();
        }

        // GET: api/reservation/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Reservation>> GetReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Card)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return reservation;
        }

        // GET: api/reservations/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetUserReservations(string userId) // userId все еще string для маршрута
        {
            // Здесь нужно преобразовать string userId в Guid
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return BadRequest("Некорректный формат идентификатора пользователя.");
            }

            return await _context.Reservations
                .Include(r => r.Card)
                .Where(r => r.UserId == userGuid)
                .ToListAsync();
        }

        // POST: api/reservations
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Reservation>> PostReservation(ReservationDto reservationDto)
        {
            // Получаем UserId из JWT токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Требуется авторизация.");
            }

            // Преобразуем string UserId из клейма в Guid
            if (!Guid.TryParse(userIdClaim, out Guid userIdGuid))
            {
                return Unauthorized("Некорректный формат идентификатора пользователя в токене.");
            }

            // Validate dates
            if (reservationDto.CheckInDate >= reservationDto.CheckOutDate)
            {
                return BadRequest("Дата выезда должна быть после даты заезда.");
            }

            if (reservationDto.CheckInDate < DateTime.Now.Date)
            {
                return BadRequest("Дата заезда не может быть в прошлом.");
            }

            // Check if card exists 
            var cardExists = await _context.Cards.AnyAsync(c => c.Id == reservationDto.CardId);
            if (!cardExists)
            {
                return BadRequest("Жилье не найдено.");
            }

            // Check for overlapping reservations 
            var hasOverlap = await _context.Reservations
                .AnyAsync(r => r.CardId == reservationDto.CardId &&
                             r.Status != ReservationStatus.Cancelled &&
                             ((reservationDto.CheckInDate >= r.CheckInDate && reservationDto.CheckInDate < r.CheckOutDate) ||
                              (reservationDto.CheckOutDate > r.CheckInDate && reservationDto.CheckOutDate <= r.CheckOutDate) ||
                              (reservationDto.CheckInDate <= r.CheckInDate && reservationDto.CheckOutDate >= r.CheckOutDate)));

            if (hasOverlap)
            {
                return BadRequest("Выбранные даты недоступны для данного жилья.");
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
                UserId = userIdGuid, // Используем userIdGuid
                CreatedAt = DateTime.UtcNow,
                Status = ReservationStatus.Pending
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
        }

        // PUT: api/reservations/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutReservation(int id, ReservationUpdateDto reservationUpdateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Требуется авторизация.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userIdGuid))
            {
                return Unauthorized("Некорректный формат идентификатора пользователя в токене.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Check if user owns this reservation 
            if (reservation.UserId != userIdGuid) // Сравниваем Guid
            {
                return Forbid("Вы можете изменять только свои резервации.");
            }

            // Update only provided fields 
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

            // Validate dates if they were updated 
            if (reservation.CheckInDate >= reservation.CheckOutDate)
            {
                return BadRequest("Дата выезда должна быть после даты заезда.");
            }

            _context.Update(reservation);

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

        // DELETE: api/reservations/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Требуется авторизация.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userIdGuid))
            {
                return Unauthorized("Некорректный формат идентификатора пользователя в токене.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Check if user owns this reservation 
            if (reservation.UserId != userIdGuid) // Сравниваем Guid
            {
                return Forbid("Вы можете отменять только свои резервации.");
            }

            // Instead of deleting, mark as cancelled 
            reservation.Status = ReservationStatus.Cancelled;
            _context.Update(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/reservation/card/{cardId}/availability
        [HttpGet("card/{cardId}/availability")]
        public async Task<ActionResult<IEnumerable<object>>> GetCardAvailability(int cardId, DateTime? from = null, DateTime? to = null)
        {
            var fromDate = from ?? DateTime.Now.Date;
            var toDate = to ?? DateTime.Now.Date.AddMonths(6);

            var bookedDates = await _context.Reservations
                .Where(r => r.CardId == cardId &&
                           r.Status != ReservationStatus.Cancelled &&
                           r.CheckInDate < toDate &&
                           r.CheckOutDate > fromDate)
                .Select(r => new
                {
                    CheckIn = r.CheckInDate,
                    CheckOut = r.CheckOutDate
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