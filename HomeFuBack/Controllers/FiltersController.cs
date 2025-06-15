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
        public async Task<ActionResult<IEnumerable<CardResponseDto>>> FilterByAvailability([FromQuery] FiltersDto filterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Подготовка дат для запроса
            var checkIn = filterDto.CheckInDate?.Date ?? DateTime.UtcNow.Date;
            var checkOut = filterDto.CheckOutDate?.Date ?? checkIn.AddYears(1);

            //if (checkIn >= checkOut)
            //{
            //    return BadRequest("Дата выезда должна быть после даты заезда.");
            //}
            //if (checkIn < DateTime.UtcNow.Date)
            //{
            //    return BadRequest("Дата заезда не может быть в прошлом.");
            //}

            // 2. Расчет общего количества гостей
            var totalGuestsForCapacity = filterDto.Adults + filterDto.Children;
            // Младенцы (Infants) не учитываются в общей вместимости "NumberOfGuests" по умолчанию.
            if (totalGuestsForCapacity == 0 && (filterDto.Infants > 0 || filterDto.Pets > 0))
            {
                totalGuestsForCapacity = 1;
            }
            else if (totalGuestsForCapacity == 0) // Если вообще никого нет
            {
                return BadRequest("Необходимо указать хотя бы 1 взрослого, ребенка, младенца или питомца.");
            }


            // 3. Строим LINQ-запрос к карточкам
            var query = _context.Cards
                .Include(c => c.CardDetail)
                .Include(c => c.Location) // Для LocationId и LocationName
                .Include(c => c.CardCategories) // Для CategoryIds
                    .ThenInclude(cc => cc.Category)
                .Where(c => !c.IsDeleted) // Исключаем удаленные карточки
                .AsQueryable();


            // 4. Фильтрация по вместимости (NumberOfGuests)
            query = query.Where(c =>
                c.CardDetail != null &&
                c.CardDetail.NumberOfGuests >= totalGuestsForCapacity
            );

            // 5. Фильтрация по питомцам
            //if (filterDto.Pets > 0)
            //{
            //    query = query.Where(c => c.CardDetail != null && c.CardDetail.AllowsPets);
            //}

            // 6. Фильтрация по поисковому запросу (SearchTerm)
            if (!string.IsNullOrWhiteSpace(filterDto.SearchTerm))
            {
                // Убираем лишние пробелы и приводим к нижнему регистру для поиска без учета регистра
                var searchTerm = filterDto.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(searchTerm));
            }

            // 7. Фильрация по ID Локации
            if (filterDto.LocationId.HasValue && filterDto.LocationId.Value > 0) 
            {
                query = query.Where(c => c.LocationId == filterDto.LocationId.Value);
            }

            // Фильтруем карточки, которые имеют EndDate запрашиваемой CheckOutDate
            if (filterDto.CheckOutDate.HasValue)
            {
                // Фильтруем карточки:
                // 1. Убеждаемся, что EndDate не равно null (c.EndDate.HasValue)
                // 2. Сравниваем только часть с датой (c.EndDate.Value.Date)
                query = query.Where(c => c.EndDate.HasValue && c.EndDate.Value.Date == filterDto.CheckOutDate.Value.Date);
            }

            // Фильтруем карточки, которые имеют StartDate запрашиваемой CheckInDate
            if (filterDto.CheckInDate.HasValue)
            {
                query = query.Where(c => c.StartDate.Date == filterDto.CheckInDate.Value.Date);
            }

            // 8. Фильтрация по доступности дат (отсутствие пересекающихся резерваций)
            query = query.Where(card => !_context.Reservations.Any(reservation =>
                reservation.CardId == card.Id &&
                reservation.Status != ReservationStatus.Cancelled &&
                reservation.Status != ReservationStatus.Completed &&
                (
                    (checkIn < reservation.CheckOutDate && checkOut > reservation.CheckInDate)
                )
            ));

            // 9. Выполняем запрос к базе данных
            var availableCards = await query.ToListAsync();

            // 10. Прямой маппинг результата в CardResponseDto (вместо отдельного метода)
            var responseDtos = availableCards.Select(card => new CardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                LocationId = card.LocationId,
                LocationName = card.Location?.Name ?? string.Empty,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                Rating = card.Rating, // Используем Rating из самой Card
                Price = card.Price,
                IsDeleted = card.IsDeleted,
                ImageUrls = card.ImageUrls ?? new List<string>(),
                CategoryIds = card.CardCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<int>()
            }).ToList();

            return Ok(responseDtos);
        }
    }
}