using HomeFuBack.Data;
using HomeFuBack.Data.DTO;
using HomeFuBack.Helpers;
using HomeFuBack.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HomeFuBack.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] // Весь контроллер требует аутентификации
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Для работы с файлами

        public UsersController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private IActionResult ValidateUserOwnership(Guid id)
        {
            var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdFromToken) || !Guid.TryParse(userIdFromToken, out Guid tokenUserId))
            {
                return Unauthorized("Invalid token.");
            }

            if (tokenUserId != id)
            {
                return Unauthorized("Token does not belong to this user.");
            }

            return null; // Возвращаем null, если проверка прошла успешно
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return Unauthorized(); // Используем Unauthorized() хелпер

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, [FromBody] User updatedUser)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return unauthorizedResult;

            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Обновляем только разрешенные поля
            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.EmergencyContactName = updatedUser.EmergencyContactName;
            user.EmergencyContactPhone = updatedUser.EmergencyContactPhone;
            user.BirthDate = updatedUser.BirthDate;
            user.Gender = updatedUser.Gender;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // PATCH: api/users/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchUser(Guid id, [FromBody] UserPatchDto updatedUserDto)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return Unauthorized();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var patchDoc = new JsonPatchDocument<User>();

            if (updatedUserDto.FirstName != null) patchDoc.Replace(u => u.FirstName, updatedUserDto.FirstName);
            if (updatedUserDto.LastName != null) patchDoc.Replace(u => u.LastName, updatedUserDto.LastName);
            if (updatedUserDto.PhoneNumber != null) patchDoc.Replace(u => u.PhoneNumber, updatedUserDto.PhoneNumber);
            if (updatedUserDto.Address != null) patchDoc.Replace(u => u.Address, updatedUserDto.Address);
            if (updatedUserDto.EmergencyContactName != null) patchDoc.Replace(u => u.EmergencyContactName, updatedUserDto.EmergencyContactName);
            if (updatedUserDto.EmergencyContactPhone != null) patchDoc.Replace(u => u.EmergencyContactPhone, updatedUserDto.EmergencyContactPhone);
            if (updatedUserDto.BirthDate != null) patchDoc.Replace(u => u.BirthDate, updatedUserDto.BirthDate);
            if (updatedUserDto.Gender != null) patchDoc.Replace(u => u.Gender, updatedUserDto.Gender);
            if (updatedUserDto.Email != null) patchDoc.Replace(u => u.Email, updatedUserDto.Email);
            if (updatedUserDto.Password != null)
            {
                // Внимание: Обновление пароля обычно делается через отдельный эндпоинт с хэшированием
                patchDoc.Replace(u => u.Password, updatedUserDto.Password);
            }

            patchDoc.ApplyTo(user, error => ModelState.AddModelError("PatchError", error.ErrorMessage));

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: api/users/{id}/photo
        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadProfilePhoto(Guid id, IFormFile file)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return unauthorizedResult;

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (file != null && file.Length > 0)
            {
                // Проверка типа файла (опционально)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Неподдерживаемый формат изображения.");
                }

                // Генерация уникального имени файла
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var imagePath = Path.Combine(_environment.WebRootPath, "images/profiles", uniqueFileName);

                // Сохранение файла на сервере
                await using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Обновление пути к изображению в базе данных
                user.ProfileImageUrl = $"/images/profiles/{uniqueFileName}";
                _context.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { imageUrl = user.ProfileImageUrl });
            }

            return BadRequest("Файл не был загружен или является пустым.");
        }

        // DELETE: api/users/{id}/photo
        [HttpDelete("{id}/photo")]
        public async Task<IActionResult> DeleteProfilePhoto(Guid id)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return unauthorizedResult;

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                user.ProfileImageUrl = null;
                _context.Update(user);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return NotFound("No profile photo found for this user.");
        }

        // PUT: api/users/{id}/email
        [HttpPut("{id}/email")]
        public async Task<IActionResult> UpdateEmail(Guid id, [FromBody] string newEmail)
        {
            var unauthorizedResult = ValidateUserOwnership(id);
            if (unauthorizedResult != null) return unauthorizedResult;

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(newEmail) && newEmail != user.Email)
            {
                // В реальном приложении здесь должна быть логика проверки уникальности email,
                // отправка кода подтверждения и т.д.
                user.Email = newEmail;
                _context.Update(user);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return BadRequest("Invalid or same email provided.");
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}