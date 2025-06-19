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
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Добавлен для случая, когда нет доступных карточек
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> FilterByAvailability([FromQuery] FiltersDto filterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Подготовка дат для запроса
            // Если CheckInDate не указана, используем текущую дату UTC
            var checkIn = filterDto.CheckInDate?.Date ?? DateTime.UtcNow.Date;
            // Если CheckOutDate не указана, устанавливаем её на 1 год вперед от checkIn
            var checkOut = filterDto.CheckOutDate?.Date ?? checkIn.AddYears(1);

            // Валидация дат
            if (checkIn >= checkOut)
            {
                return BadRequest("Дата выезда должна быть после даты заезда.");
            }
            if (checkIn < DateTime.UtcNow.Date)
            {
                return BadRequest("Дата заезда не может быть в прошлом.");
            }

            // 2. Расчет общего количества гостей для вместимости
            var totalGuestsForCapacity = filterDto.Adults + filterDto.Children;

            if (totalGuestsForCapacity == 0 && (filterDto.Infants > 0 || filterDto.Pets > 0))
            {
                totalGuestsForCapacity = 1;
            }
            else if (totalGuestsForCapacity == 0 && filterDto.Infants == 0 && filterDto.Pets == 0)
            {
                return BadRequest("Необходимо указать хотя бы 1 взрослого, ребенка, младенца или питомца.");
            }


            // 3. Строим LINQ-запрос к карточкам, включаем необходимые связанные данные
            var query = _context.Cards
                .Include(c => c.CardDetail)
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                    .ThenInclude(cc => cc.Category)
                .Where(c => !c.IsDeleted) // Исключаем удаленные карточки
                .AsQueryable();


            // 4. Фильтрация по вместимости (NumberOfGuests)
            query = query.Where(c =>
                c.CardDetail != null && // Добавлена проверка на null для CardDetail для безопасности
                c.CardDetail.NumberOfGuests >= totalGuestsForCapacity
            );

            // 5. Фильтрация по питомцам (если параметр раскомментирован и включен)
            // if (filterDto.Pets > 0)
            // {
            //     query = query.Where(c => c.CardDetail != null && c.CardDetail.AllowsPets);
            // }

            // 6. Фильтрация по поисковому запросу (SearchTerm)
            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                var searchTerm = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(searchTerm));
            }

            // 7. Фильтрация по ID Локации
            if (filterDto.LocationId.HasValue && filterDto.LocationId.Value > 0)
            {
                query = query.Where(c => c.LocationId == filterDto.LocationId.Value);
            }

            // 8. ГЛАВНАЯ ФИЛЬТРАЦИЯ по доступности дат:
            // Исключаем карточки, у которых есть АКТИВНЫЕ (не отмененные/завершенные) резервации,
            // которые пересекаются с запрашиваемым периодом.
            query = query.Where(card => !_context.Reservations.Any(reservation =>
                reservation.CardId == card.Id &&
                reservation.Status != ReservationStatus.Cancelled &&
                reservation.Status != ReservationStatus.Completed &&
                // Проверка на пересечение диапазонов дат:
                // [checkIn, checkOut) пересекается с [reservation.CheckInDate, reservation.CheckOutDate)
                (
                    (checkIn < reservation.CheckOutDate && checkOut > reservation.CheckInDate)
                )
            ));

            // 9. Выполняем запрос к базе данных
            var availableCards = await query.ToListAsync();

            // 10. Проверка на наличие результатов и возврат соответствующего ответа
            if (!availableCards.Any())
            {
                // Если список пуст, это значит, что нет карточек,
                // соответствующих ВСЕМ условиям фильтрации, включая доступность по резервациям.
                return NotFound("На выбранные даты и с учетом заданных критериев нет доступных вариантов. Попробуйте изменить даты или параметры поиска.");
            }

            // 11. Прямой маппинг результата в CardResponseDto
            var responseDtos = availableCards.Select(card => new CardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                LocationId = card.LocationId,
                LocationName = card.Location?.Name ?? string.Empty,
                StartDate = card.StartDate, // Эти поля все еще полезны для отображения
                EndDate = card.EndDate,     // общего периода карточки во фронтенде
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