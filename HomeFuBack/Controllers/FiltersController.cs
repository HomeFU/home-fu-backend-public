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

        [HttpGet("availability")] // Изменено на более общее название, но 'availability' все еще уместно
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> FilterByAvailability([FromQuery] FiltersDto filterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Определяем, запрашивается ли полная фильтрация по доступности или только общая фильтрация
            // Если CheckInDate ИЛИ CheckOutDate ИЛИ Adults/Children/Infants/Pets указаны, то это полная фильтрация.
            // Иначе (только LocationId или SearchTerm), это общая фильтрация.
            bool isFullAvailabilityCheck = filterDto.CheckInDate.HasValue ||
                                           filterDto.CheckOutDate.HasValue ||
                                           filterDto.Adults.HasValue ||
                                           filterDto.Children.HasValue ||
                                           filterDto.Infants.HasValue ||
                                           filterDto.Pets.HasValue;


            // 1. Подготовка дат для запроса (только если это полная проверка доступности)
            DateTime checkIn = DateTime.MinValue; // Инициализируем минимальными значениями
            DateTime checkOut = DateTime.MinValue;

            if (isFullAvailabilityCheck)
            {
                checkIn = filterDto.CheckInDate?.Date ?? DateTime.UtcNow.Date;
                checkOut = filterDto.CheckOutDate?.Date ?? checkIn.AddYears(1); // Большая дефолтная дата

                // Валидация дат для полной проверки
                if (checkIn >= checkOut)
                {
                    return BadRequest("Дата выезда должна быть после даты заезда.");
                }
                if (checkIn < DateTime.UtcNow.Date)
                {
                    return BadRequest("Дата заезда не может быть в прошлом.");
                }
            }


            // 2. Расчет общего количества гостей для вместимости (только если это полная проверка доступности)
            int totalGuestsForCapacity = 0; // Инициализируем 0, чтобы не влияло на базовый запрос

            if (isFullAvailabilityCheck)
            {
                // Проверка, что есть хотя бы 1 взрослый
                if (filterDto.Adults.GetValueOrDefault(0) == 0)
                {
                    return BadRequest("Для бронирования необходимо указать хотя бы 1 взрослого.");
                }

                // Расчитываем общую вместимость, включая всех гостей
                totalGuestsForCapacity = filterDto.Adults.Value + // Используем .Value, т.к. уже проверили, что Adults >= 1
                                         filterDto.Children.GetValueOrDefault(0) +
                                         filterDto.Infants.GetValueOrDefault(0); // Младенцы тоже учитываются в capacity
            }


            // 3. Строим LINQ-запрос к карточкам, включаем необходимые связанные данные
            var query = _context.Cards
                .Include(c => c.CardDetail)
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .Where(c => !c.IsDeleted) // Исключаем удаленные карточки
                .AsQueryable();


            // 4. Фильтрация по вместимости (NumberOfGuests) (только при полной проверке доступности)
            if (isFullAvailabilityCheck)
            {
                query = query.Where(c =>
                    c.CardDetail != null &&
                    c.CardDetail.NumberOfGuests >= totalGuestsForCapacity
                );
            }

            // 5. Фильтрация по питомцам 
            // Применяем только если это полная проверка доступности ИЛИ Pets явно указаны
            //if (filterDto.Pets.HasValue && filterDto.Pets.Value > 0)
            //{
            //    query = query.Where(c => c.CardDetail != null && c.CardDetail.AllowsPets);
            //}


            // 6. Фильтрация по поисковому запросу (SearchTerm) - всегда применяется, если есть
            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                var searchTerm = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(searchTerm) ||
                                       (c.Location != null && c.Location.Name.ToLower().Contains(searchTerm))); // Добавлено: поиск по названию локации
            }

            // 7. Фильтрация по ID Локации - всегда применяется, если есть
            if (filterDto.LocationId.HasValue && filterDto.LocationId.Value > 0)
            {
                query = query.Where(c => c.LocationId == filterDto.LocationId.Value);
            }

            // 8. ГЛАВНАЯ ФИЛЬТРАЦИЯ по доступности дат: (только если это полная проверка доступности)
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

            // 10. Проверка на наличие результатов и возврат соответствующего ответа
            if (!availableCards.Any())
            {
                return NotFound("На выбранные даты и с учетом заданных критериев нет доступных вариантов. Попробуйте изменить даты или параметры поиска.");
            }

            // 11. Прямой маппинг результата в CardResponseDto
            var responseDtos = availableCards.Select(card => new CardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                LocationId = card.LocationId,
                LocationName = card.Location?.Name ?? string.Empty,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                Rating = card.Rating,
                Price = card.Price,
                IsDeleted = card.IsDeleted,
                ImageUrls = card.ImageUrls ?? new List<string>(),
                CategoryIds = card.CardCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<int>()
            }).ToList();

            return Ok(responseDtos);
        }
    }
}

