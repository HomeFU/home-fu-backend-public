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
    // [Authorize]
    public class CardDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CardDetailsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

 
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
                .Include(cd => cd.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound();
            }

            return Ok(MapCardDetailToResponseDto(cardDetail));
        }

        // POST: api/carddetails
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCardDetail(int id)
        {
            // Загружаем CardDetail и связанную Card, так как IsDeleted находится в Card
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Card)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound($"Детальная карточка с ID {id} не найдена.");
            }

            var card = cardDetail.Card;
            if (card == null)
            {
                return NotFound($"Связанная основная карточка для CardDetail с ID {id} не найдена.");
            }

            card.IsDeleted = true;

            _context.Entry(card).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Обработка конфликтов параллельного обновления, если применимо
                if (!CardDetailExists(id)) // Проверяем, существует ли CardDetail (или Card)
                {
                    return NotFound();
                }
                else
                {
                    throw; // Что-то другое пошло не так
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при мягком удалении CardDetail {id}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при мягком удалении.");
            }

            return NoContent(); // 204 No Content - успешное выполнение
        }
        
        // PATCH: api/carddetails/{id}/restore
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreCardDetail(int id)
        {
            // 1. Находим CardDetail и связанную Card
            var cardDetail = await _context.CardDetails
                .Include(cd => cd.Card) // Обязательно загружаем связанную Card
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (cardDetail == null)
            {
                return NotFound($"Детальная карточка с ID {id} не найдена.");
            }

            var card = cardDetail.Card;
            if (card == null)
            {
                return NotFound($"Связанная основная карточка для CardDetail с ID {id} не найдена.");
            }

            // 2. Проверяем, действительно ли карточка была "удалена" (IsDeleted = true)
            if (!card.IsDeleted)
            {
                return BadRequest($"Карточка с ID {id} уже активна и не требует восстановления.");
            }

            // 3. Устанавливаем IsDeleted в false
            card.IsDeleted = false;

            // 4. Отмечаем Card как измененную
            // Хотя EF Core часто отслеживает изменения в загруженных сущностях, явное указание не повредит
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
                Console.WriteLine($"Ошибка при восстановлении CardDetail {id}: {ex.Message}");
                return StatusCode(500, "Внутренняя ошибка сервера при восстановлении карточки.");
            }

            return NoContent(); // 204 No Content - успешное выполнение без возврата содержимого
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

            // Вспомогательный метод для маппинга Comment в CommentResponseDto (скопируйте из CommentsController)
            Func<Comment, CommentResponseDto> mapCommentToResponseDto = (comment) =>
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
                    OverallRating = comment.OverallRating
                };
            };

            return new CardDetailResponseDto
            {
                Id = cardDetail.Id,
                NumberOfGuests = cardDetail.NumberOfGuests,
                NumberOfBedrooms = cardDetail.NumberOfBedrooms,
                NumberOfBeds = cardDetail.NumberOfBeds,
                NumberOfBathrooms = cardDetail.NumberOfBathrooms,
                HostId = cardDetail.HostId,
                HostName = cardDetail.Host.FirstName!,
                HostAvatarUrl = cardDetail.Host?.ProfileImageUrl,
                HostMail = cardDetail.Host!.Email,
                HostNum = cardDetail.Host.PhoneNumber!,
                Description = cardDetail.Description,
                Latitude = cardDetail.Latitude,
                Longitude = cardDetail.Longitude,
                Amenities = cardDetail.CardDetailAmenities?.Select(cda => new AmenityResponseDto
                {
                    Id = cda.Amenity.Id,
                    Name = cda.Amenity.Name,
                    ImageUrl = cda.Amenity.IconPath
                }).ToList() ?? new List<AmenityResponseDto>(),

                Ratings = cardDetail.Ratings != null ? new RatingDto
                {
                    Cleanliness = cardDetail.Ratings.Cleanliness,
                    Accuracy = cardDetail.Ratings.Accuracy,
                    CheckIn = cardDetail.Ratings.CheckIn,
                    Communication = cardDetail.Ratings.Communication,
                    Location = cardDetail.Ratings.Location,
                    Value = cardDetail.Ratings.Value
                } : null,

                Card = cardDetail.Card != null ? new CardResponseDto
                {
                    Id = cardDetail.Card.Id,
                    Name = cardDetail.Card.Name,
                    LocationId = cardDetail.Card.LocationId,
                    LocationName = cardDetail.Card.Location?.Name!,
                    StartDate = cardDetail.Card.StartDate,
                    EndDate = cardDetail.Card.EndDate,
                    Rating = cardDetail.Card.Rating,
                    Price = cardDetail.Card.Price,
                    IsDeleted = cardDetail.Card.IsDeleted,
                    ImageUrls = cardDetail.Card.ImageUrls,
                    CategoryIds = cardDetail.Card.CardCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<int>()
                } : null,

                // НОВОЕ: Заполняем список отзывов
                Reviews = cardDetail.Comments?.Select(mapCommentToResponseDto).ToList() ?? new List<CommentResponseDto>()
            };
        }
    }
}