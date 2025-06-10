using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Models.Housing;// Для Amenity и Rating
using HomeFuBack.Models.Users; // Для User
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using HomeFuBack.Helpers; // Для UniqueFileName

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/carddetails")]
    // [Authorize] // Возможно, только авторизованные пользователи могут создавать/изменять
    public class CardDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CardDetailsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/carddetails
        /// <summary>
        /// Получает список всех детальных карточек с их связанными данными (Card, Rating, User, Amenities).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CardDetailResponseDto>>> GetCardDetails()
        {
            var cardDetails = await _context.CardDetails
                .Include(cd => cd.Ratings)
                .Include(cd => cd.Host) // Включаем связанного Host (User)
                .Include(cd => cd.CardDetailAmenities)
                    .ThenInclude(cda => cda.Amenity)
                .Include(cd => cd.Card) // Включаем связанную Card
                    .ThenInclude(c => c.Location) // Включаем Location для Card
                .Include(cd => cd.Card) // Снова включаем Card для отдельного ThenInclude
                    .ThenInclude(c => c.CardCategories)
                .ToListAsync();

            var response = cardDetails.Select(cd => MapCardDetailToResponseDto(cd)).ToList();
            return Ok(response);
        }

        // GET: api/carddetails/{id}
        /// <summary>
        /// Получает детальную карточку по ее ID с ее связанными данными (Card, Rating, User, Amenities).
        /// </summary>
        /// <param name="id">ID детальной карточки.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<CardDetailResponseDto>> GetCardDetail(int id)
        {
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Ratings)
                .Include(cd => cd.Host) // Включаем связанного Host (User)
                .Include(cd => cd.CardDetailAmenities)
                    .ThenInclude(cda => cda.Amenity)
                .Include(cd => cd.Card) // Включаем связанную Card
                    .ThenInclude(c => c.Location) // Включаем Location для Card
                .Include(cd => cd.Card) // Снова включаем Card для отдельного ThenInclude
                    .ThenInclude(c => c.CardCategories)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound();
            }

            return Ok(MapCardDetailToResponseDto(cardDetail));
        }

        // POST: api/carddetails
        /// <summary>
        /// Создает новую детальную карточку, а также связанную с ней основную карточку и записи оценок.
        /// </summary>
        /// <param name="dto">Данные для создания детальной карточки.</param>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CardDetailResponseDto>> CreateCardDetail([FromForm] CardDetailCreateDto dto)
        {
            // 1. Проверка Location для Card
            var location = await _context.Locations.FindAsync(dto.LocationId);
            if (location == null)
            {
                return BadRequest($"Локация с ID {dto.LocationId} не найдена.");
            }

            // 2. Проверка HostId (User)
            var hostUser = await _context.Users.FindAsync(dto.HostId);
            if (hostUser == null)
            {
                return BadRequest($"Пользователь (хост) с ID {dto.HostId} не найден.");
            }

            // 3. Создание Card
            var card = new Card
            {
                Name = dto.CardName,
                LocationId = dto.LocationId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Rating = dto.Rating,
                Price = dto.Price,
                IsDeleted = false,
                ImageUrls = new List<string>(),
                CardCategories = new List<CardCategory>()
            };

            // Сохранение изображений карточки
            if (dto.CardImages != null && dto.CardImages.Any())
            {
                foreach (var imageFile in dto.CardImages)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await SaveImage(imageFile, "cards");
                        card.ImageUrls.Add(imageUrl);
                    }
                }
            }

            // Добавляем Card в контекст
            _context.Cards.Add(card);
            await _context.SaveChangesAsync(); // Сохраняем Card, чтобы получить ее Id (Primary Key)

            // 4. Создание CardDetail
            var cardDetail = new CardDetail
            {
                // Поскольку Id CardDetail должен совпадать с Id Card (Shared Primary Key)
                // И CardDetail.Id теперь также является внешним ключом к Card
                // Замените Id = card.Id на присвоение после сохранения Card.
                // Или позвольте EF Core управлять этим, если OneToOne настроен правильно.
                // Важно, чтобы cardDetail.Id было установлено ДО того, как она будет использоваться
                // в Rating.CardDetailId
                Id = card.Id, // <--- ЭТОТ ШАГ ВАЖЕН ДЛЯ Shared Primary Key
                NumberOfGuests = dto.NumberOfGuests,
                NumberOfBedrooms = dto.NumberOfBedrooms,
                NumberOfBeds = dto.NumberOfBeds,
                NumberOfBathrooms = dto.NumberOfBathrooms,
                HostId = dto.HostId,
                Description = dto.Description,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                // RatingId и Ratings здесь не нужны, так как Rating будет ссылаться на CardDetail
                // RatingId = rating.Id, // УДАЛИТЬ или закомментировать
                // Ratings = rating // УДАЛИТЬ или закомментировать
            };
            _context.CardDetails.Add(cardDetail); // Добавляем CardDetail в контекст

            // 5. Создание Rating
            // Теперь, когда cardDetail создан и имеет Id (благодаря shared primary key с Card),
            // мы можем создать Rating и связать его.
            var rating = new Rating
            {
                CardDetailId = cardDetail.Id, // <-- ЭТО КЛЮЧЕВОЕ ИЗМЕНЕНИЕ! Устанавливаем FK.
                Cleanliness = dto.InitialCleanliness,
                Accuracy = dto.InitialAccuracy,
                CheckIn = dto.InitialCheckIn,
                Communication = dto.InitialCommunication,
                Location = dto.InitialLocationRating,
                Value = dto.InitialValue
            };
            _context.Ratings.Add(rating); // Добавляем Rating в контекст

            // 6. Связываем CardDetail с Amenity через CardDetailAmenities
            if (dto.AmenityIds != null && dto.AmenityIds.Any())
            {
                foreach (var amenityId in dto.AmenityIds)
                {
                    var amenity = await _context.Amenities.FindAsync(amenityId);
                    if (amenity == null)
                    {
                        // При ошибке, удаляем уже добавленные сущности
                        //_context.Cards.Remove(card);
                        //_context.CardDetails.Remove(cardDetail);
                        //_context.Ratings.Remove(rating);
                        return BadRequest($"Удобство с ID {amenityId} не найдено.");
                    }
                    cardDetail.CardDetailAmenities.Add(new CardDetailAmenity { AmenityId = amenityId });
                }
            }

            try
            {
                await _context.SaveChangesAsync(); // <-- Единое сохранение всех связанных сущностей
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании CardDetail: {ex.Message}");
                // Если что-то пошло не так, можно попытаться откатить изменения или залогировать.
                // _context.Cards.Remove(card); // Эти строки, возможно, уже не нужны, так как транзакция
                // _context.Ratings.Remove(rating); // должна была бы откатиться в случае DbUpdateException,
                // _context.CardDetails.Remove(cardDetail); // но явное удаление иногда полезно при отладке.
                return StatusCode(500, $"Внутренняя ошибка сервера при создании детальной карточки: {ex.Message}");
            }

            // Загружаем связанные данные для DTO ответа
            // Теперь, когда все сохранено, можно загрузить отношения.
            // Если cardDetail уже имеет Rating (через cardDetail.Ratings),
            // то можно просто использовать его для маппинга.
            await _context.Entry(cardDetail)
                .Reference(cd => cd.Ratings)
                .LoadAsync();
            await _context.Entry(cardDetail)
                .Reference(cd => cd.Host)
                .LoadAsync();
            await _context.Entry(cardDetail)
                .Reference(cd => cd.Card)
                .Query()
                .Include(c => c.Location)
                .Include(c => c.CardCategories)
                .LoadAsync();
            await _context.Entry(cardDetail)
                .Collection(cd => cd.CardDetailAmenities)
                .Query()
                .Include(cda => cda.Amenity)
                .LoadAsync();

            return CreatedAtAction(nameof(GetCardDetail), new { id = cardDetail.Id }, MapCardDetailToResponseDto(cardDetail));
        }

        // PUT: api/carddetails/{id}
        /// <summary>
        /// Обновляет существующую детальную карточку, а также связанную с ней основную карточку и записи оценок.
        /// </summary>
        /// <param name="id">ID детальной карточки.</param>
        /// <param name="dto">Обновленные данные детальной карточки.</param>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCardDetail(int id, [FromForm] CardDetailUpdateDto dto)
        {
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Card)
                .Include(cd => cd.Ratings)
                .Include(cd => cd.Host)
                .Include(cd => cd.CardDetailAmenities)
                    .ThenInclude(cda => cda.Amenity)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound($"Детальная карточка с ID {id} не найдена.");
            }

            var card = cardDetail.Card;
            if (card == null)
            {
                return NotFound($"Связанная основная карточка для CardDetail с ID {id} не найдена. (Это может быть ошибкой в данных, если CardDetail есть, а Card нет).");
            }
            var rating = cardDetail.Ratings; // Получаем Rating

            // --- Обновление CardDetail ---
            if (dto.NumberOfGuests.HasValue) cardDetail.NumberOfGuests = dto.NumberOfGuests.Value;
            if (dto.NumberOfBedrooms.HasValue) cardDetail.NumberOfBedrooms = dto.NumberOfBedrooms.Value;
            if (dto.NumberOfBeds.HasValue) cardDetail.NumberOfBeds = dto.NumberOfBeds.Value;
            if (dto.NumberOfBathrooms.HasValue) cardDetail.NumberOfBathrooms = dto.NumberOfBathrooms.Value;

            if (dto.HostId.HasValue)
            {
                var newHost = await _context.Users.FindAsync(dto.HostId.Value);
                if (newHost == null)
                {
                    return BadRequest($"Новый пользователь (хост) с ID {dto.HostId.Value} не найден.");
                }
                cardDetail.HostId = dto.HostId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description)) cardDetail.Description = dto.Description;
            if (dto.Latitude.HasValue) cardDetail.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) cardDetail.Longitude = dto.Longitude.Value;

            // Обновление удобств (CardDetailAmenities)
            if (dto.AmenityIds != null) // Если amenityIds передан, это новый полный список удобств
            {
                var existingAmenityIds = cardDetail.CardDetailAmenities.Select(cda => cda.AmenityId).ToList();
                var amenitiesToAdd = dto.AmenityIds.Except(existingAmenityIds).ToList();
                var amenitiesToRemove = existingAmenityIds.Except(dto.AmenityIds).ToList();

                foreach (var amenityId in amenitiesToRemove)
                {
                    var cdaToRemove = cardDetail.CardDetailAmenities.FirstOrDefault(cda => cda.AmenityId == amenityId);
                    if (cdaToRemove != null)
                    {
                        _context.CardDetailAmenities.Remove(cdaToRemove);
                    }
                }

                foreach (var amenityId in amenitiesToAdd)
                {
                    var amenity = await _context.Amenities.FindAsync(amenityId);
                    if (amenity == null)
                    {
                        return BadRequest($"Удобство с ID {amenityId} не найдено.");
                    }
                    cardDetail.CardDetailAmenities.Add(new CardDetailAmenity { AmenityId = amenityId });
                }
            }
            else if (dto.AmenitiesToRemove != null && dto.AmenitiesToRemove.Any()) // Если передан список для удаления
            {
                foreach (var amenityIdToRemove in dto.AmenitiesToRemove)
                {
                    var cdaToRemove = cardDetail.CardDetailAmenities.FirstOrDefault(cda => cda.AmenityId == amenityIdToRemove);
                    if (cdaToRemove != null)
                    {
                        _context.CardDetailAmenities.Remove(cdaToRemove);
                    }
                }
            }

            // --- Обновление Card ---
            if (!string.IsNullOrWhiteSpace(dto.CardName)) card.Name = dto.CardName;
            if (dto.LocationId.HasValue)
            {
                var newLocation = await _context.Locations.FindAsync(dto.LocationId.Value);
                if (newLocation == null)
                {
                    return BadRequest($"Локация с ID {dto.LocationId.Value} не найдена.");
                }
                card.LocationId = dto.LocationId.Value;
            }
            if (dto.StartDate.HasValue) card.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) card.EndDate = dto.EndDate.Value;
            if (dto.Rating.HasValue) card.Rating = dto.Rating.Value;
            if (dto.Price.HasValue) card.Price = dto.Price.Value;
            if (dto.IsDeleted.HasValue) card.IsDeleted = dto.IsDeleted.Value;

            // Обновление изображений карточки
            if (dto.CardImages != null && dto.CardImages.Any())
            {
                foreach (var imageFile in dto.CardImages)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await SaveImage(imageFile, "cards");
                        card.ImageUrls.Add(imageUrl);
                    }
                }
            }
            if (dto.ImageUrlsToRemove != null && dto.ImageUrlsToRemove.Any())
            {
                foreach (var urlToRemove in dto.ImageUrlsToRemove)
                {
                    if (card.ImageUrls.Contains(urlToRemove))
                    {
                        DeleteImage(urlToRemove);
                        card.ImageUrls.Remove(urlToRemove);
                    }
                }
            }

            // Обновление категорий Card (если CardCategories есть в CardDto, нужно будет обновить)
            // В вашем DTO (CardDetailUpdateDto) нет полей для CategoryIds для Card,
            // поэтому я не включил логику обновления категорий Card здесь.
            // Если они вам нужны, добавьте List<int>? CardCategoryIds в CardDetailUpdateDto.

            // --- Обновление Rating ---
            if (rating != null) // Если Rating существует (должен существовать, т.к. создается с CardDetail)
            {
                if (dto.Cleanliness.HasValue) rating.Cleanliness = dto.Cleanliness.Value;
                if (dto.Accuracy.HasValue) rating.Accuracy = dto.Accuracy.Value;
                if (dto.CheckIn.HasValue) rating.CheckIn = dto.CheckIn.Value;
                if (dto.Communication.HasValue) rating.Communication = dto.Communication.Value;
                if (dto.LocationRating.HasValue) rating.Location = dto.LocationRating.Value;
                if (dto.Value.HasValue) rating.Value = dto.Value.Value;
                _context.Entry(rating).State = EntityState.Modified; // Отмечаем Rating как измененный
            }

            _context.Entry(cardDetail).State = EntityState.Modified;
            _context.Entry(card).State = EntityState.Modified;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CardDetailExists(id))
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
                Console.WriteLine($"Ошибка при обновлении CardDetail {id}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при обновлении.");
            }

            return NoContent();
        }

        // DELETE: api/carddetails/{id}
        /// <summary>
        /// Удаляет детальную карточку, а также связанные с ней основную карточку, записи оценок и файлы изображений.
        /// </summary>
        /// <param name="id">ID детальной карточки для удаления.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCardDetail(int id)
        {
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Card)
                .Include(cd => cd.Ratings)
                .Include(cd => cd.CardDetailAmenities)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound();
            }

            // Удаляем связанные изображения карточки
            if (cardDetail.Card != null && cardDetail.Card.ImageUrls != null)
            {
                foreach (var imageUrl in cardDetail.Card.ImageUrls)
                {
                    DeleteImage(imageUrl);
                }
            }

            // Удаление связанных сущностей (EF Core может сделать это каскадно, если настроено)
            // Важно: если CardDetail.Id и Card.Id одинаковые, то удаление CardDetail автоматически удалит Card.
            // Если Card.CardDetailId nullable, то при удалении CardDetail, Card.CardDetailId станет null, а Card останется.
            // В вашем случае, если Id CardDetail совпадает с Id Card, то они - одна и та же запись в логическом смысле.
            // Поэтому, если мы удаляем CardDetail, то и Card должна быть удалена.
            if (cardDetail.Card != null)
            {
                _context.Cards.Remove(cardDetail.Card);
            }
            if (cardDetail.Ratings != null)
            {
                _context.Ratings.Remove(cardDetail.Ratings);
            }
            _context.CardDetailAmenities.RemoveRange(cardDetail.CardDetailAmenities); // Удаляем связи с удобствами

            _context.CardDetails.Remove(cardDetail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CardDetailExists(int id)
        {
            return _context.CardDetails.Any(e => e.Id == id);
        }

        // --- Вспомогательные методы для работы с изображениями ---
        private async Task<string> SaveImage(IFormFile imageFile, string subfolder)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", subfolder);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var uniqueFileName = UniqueFileName.GetUniqueFileName(imageFile.FileName);
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/{subfolder}/{uniqueFileName}";
        }

        private void DeleteImage(string relativeImageUrl)
        {
            if (string.IsNullOrEmpty(relativeImageUrl)) return;

            // Удаляем начальный слеш, если он есть
            var filePathOnDisk = Path.Combine(_environment.WebRootPath, relativeImageUrl.TrimStart('/'));

            if (System.IO.File.Exists(filePathOnDisk))
            {
                try
                {
                    System.IO.File.Delete(filePathOnDisk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении изображения {filePathOnDisk}: {ex.Message}");
                    // Логирование ошибки удаления файла
                }
            }
        }

        // --- Вспомогательный метод для маппинга в DTO ответа ---
        private CardDetailResponseDto MapCardDetailToResponseDto(CardDetail cardDetail)
        {
            if (cardDetail == null) return null;

            return new CardDetailResponseDto
            {
                Id = cardDetail.Id,
                NumberOfGuests = cardDetail.NumberOfGuests,
                NumberOfBedrooms = cardDetail.NumberOfBedrooms,
                NumberOfBeds = cardDetail.NumberOfBeds,
                NumberOfBathrooms = cardDetail.NumberOfBathrooms,
                HostId = cardDetail.HostId, // Убедитесь, что Id хоста доступен
                HostName = cardDetail.Host.FirstName!, // Предполагаем, что Host имеет UserName
                HostAvatarUrl = cardDetail.Host?.ProfileImageUrl, // Предполагаем, что Host имеет ProfilePictureUrl
                Description = cardDetail.Description,
                Latitude = cardDetail.Latitude,
                Longitude = cardDetail.Longitude,
                Amenities = cardDetail.CardDetailAmenities?.Select(cda => new AmenityResponseDto // Убедитесь, что AmenityDto соответствует
                {
                    Id = cda.Amenity.Id,
                    Name = cda.Amenity.Name,
                    ImageUrl = cda.Amenity.IconPath
                }).ToList() ?? new List<AmenityResponseDto>(), // Обработка null

                // Маппинг оценок
                Ratings = cardDetail.Ratings != null ? new RatingDto // Убедитесь, что RatingDto соответствует
                {
                    Cleanliness = cardDetail.Ratings.Cleanliness,
                    Accuracy = cardDetail.Ratings.Accuracy,
                    CheckIn = cardDetail.Ratings.CheckIn,
                    Communication = cardDetail.Ratings.Communication,
                    Location = cardDetail.Ratings.Location,
                    Value = cardDetail.Ratings.Value
                } : null, // Обработка null

                Card = cardDetail.Card != null ? new CardResponseDto // Убедитесь, что CardResponseDto соответствует
                {
                    Id = cardDetail.Card.Id,
                    Name = cardDetail.Card.Name,
                    LocationId = cardDetail.Card.LocationId,
                    LocationName = cardDetail.Card.Location?.Name, // Доступ к Location.Name благодаря Include
                    StartDate = cardDetail.Card.StartDate,
                    EndDate = cardDetail.Card.EndDate,
                    Rating = cardDetail.Card.Rating,
                    Price = cardDetail.Card.Price,
                    IsDeleted = cardDetail.Card.IsDeleted,
                    ImageUrls = cardDetail.Card.ImageUrls,
                    // ВОТ ГДЕ ПРОИСХОДИТ МАППИНГ CategoryIds
                    CategoryIds = cardDetail.Card.CardCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<int>() // Обработка null
                } : null
            };
        }
    }
}