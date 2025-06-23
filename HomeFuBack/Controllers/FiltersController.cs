using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeFuBack.Models.Housing;
using Microsoft.AspNetCore.Http;

namespace HomeFuBack.Controllers
{
    [Route("api/filters")]
    [ApiController]
    public class FiltersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FiltersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> FilterByAvailability([FromQuery] FiltersDto filterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool isFullAvailabilityCheck = filterDto.CheckInDate.HasValue ||
                                           filterDto.CheckOutDate.HasValue ||
                                           filterDto.Adults.HasValue ||
                                           filterDto.Children.HasValue ||
                                           filterDto.Infants.HasValue ||
                                           filterDto.Pets.HasValue;

            DateTime today = DateTime.Today;

            DateTime checkIn = DateTime.MinValue;
            DateTime checkOut = DateTime.MinValue;

            if (isFullAvailabilityCheck)
            {
                checkIn = filterDto.CheckInDate?.Date ?? DateTime.UtcNow.Date;
                checkOut = filterDto.CheckOutDate?.Date ?? checkIn.AddYears(1);

                if (checkIn >= checkOut)
                {
                    return BadRequest("Дата выезда должна быть после даты заезда.");
                }
                if (checkIn < DateTime.UtcNow.Date)
                {
                    return BadRequest("Дата заезда не может быть в прошлом.");
                }

                if (filterDto.Adults.GetValueOrDefault(0) == 0)
                {
                    return BadRequest("Для бронирования необходимо указать хотя бы 1 взрослого.");
                }
            }

            int totalGuestsForCapacity = isFullAvailabilityCheck ?
                                         (filterDto.Adults.Value +
                                          filterDto.Children.GetValueOrDefault(0) +
                                          filterDto.Infants.GetValueOrDefault(0)) : 0;


            var query = _context.Cards
                .Include(c => c.CardDetail)
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .Where(c => !c.IsDeleted)
                .AsQueryable();


            if (isFullAvailabilityCheck)
            {
                query = query.Where(c =>
                    c.CardDetail != null &&
                    c.CardDetail.NumberOfGuests >= totalGuestsForCapacity
                );
            }

            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                var searchTerm = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(searchTerm) ||
                                         (c.Location != null && c.Location.Name.ToLower().Contains(searchTerm)));
            }

            if (filterDto.LocationId.HasValue && filterDto.LocationId.Value > 0)
            {
                query = query.Where(c => c.LocationId == filterDto.LocationId.Value);
            }

            if (isFullAvailabilityCheck)
            {
                query = query.Where(card => !_context.Reservations.Any(reservation =>
                    reservation.CardId == card.Id &&
                    reservation.Status != ReservationStatus.Cancelled &&
                    reservation.Status != ReservationStatus.Completed &&
                    (checkIn < reservation.CheckOutDate && checkOut > reservation.CheckInDate)
                ));
            }

            // 9. Выполняем запрос к базе данных
            var availableCards = await query.ToListAsync();

            // 10. Если после фильтрации не осталось карточек, возвращаем NotFound
            if (!availableCards.Any())
            {
                return NotFound("На выбранные даты и с учетом заданных критериев нет доступных вариантов. Попробуйте изменить даты или параметры поиска.");
            }

            var cardIdsInResults = availableCards.Select(c => c.Id).ToList();
            var allUpcomingReservationsForAvailableCards = await _context.Reservations
                .Where(r => cardIdsInResults.Contains(r.CardId) &&
                            (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending) &&
                            r.CheckOutDate >= today) // Используем >= today, чтобы учесть брони после сегодняшнего дня
                .OrderBy(r => r.CheckInDate)
                .ToListAsync();

            // 11. Прямой маппинг результата в CardResponseDto с вычислением следующего свободного периода
            var responseDtos = new List<CardResponseDto>();
            foreach (var card in availableCards)
            {
                DateTime displayStartDate;
                DateTime displayEndDate;

                if (isFullAvailabilityCheck)
                {
                    
                    displayStartDate = filterDto.CheckInDate!.Value; 
                    displayEndDate = filterDto.CheckOutDate!.Value;  
                }
                else
                {
                    var cardReservations = allUpcomingReservationsForAvailableCards
                        .Where(r => r.CardId == card.Id)
                        .ToList();

                    var (nextAvailableStart, nextAvailableEnd) = CalculateNextAvailablePeriod(cardReservations, DateTime.Today);
                    displayStartDate = nextAvailableStart;
                    displayEndDate = nextAvailableEnd;
                }

                responseDtos.Add(new CardResponseDto
                {
                    Id = card.Id,
                    Name = card.Name,
                    LocationId = card.LocationId,
                    LocationName = card.Location?.Name ?? string.Empty,
                    StartDate = displayStartDate, 
                    EndDate = displayEndDate,     
                    Rating = card.Rating,
                    Price = card.Price,
                    IsDeleted = card.IsDeleted,
                    ImageUrls = card.ImageUrls ?? new List<string>(),
                    CategoryIds = card.CardCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<int>()
                });
            }

            return Ok(responseDtos);
        }

        private (DateTime StartDate, DateTime EndDate) CalculateNextAvailablePeriod(
        List<Reservation> cardReservations, DateTime searchFromDate)
        {
            DateTime currentAvailableStart = searchFromDate;

            foreach (var reservation in cardReservations.OrderBy(r => r.CheckInDate))
            {
                if (reservation.CheckInDate > currentAvailableStart)
                {
                    return (currentAvailableStart, reservation.CheckInDate.AddDays(-1));
                }
                else
                {
                    currentAvailableStart = reservation.CheckOutDate.AddDays(1);
                }
            }

            return (currentAvailableStart, DateTime.Today.AddYears(1));
        }
    }
}

