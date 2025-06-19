using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using HomeFuBack.Helpers; // Убедитесь, что это действительно используется, если нет, можно удалить
using HomeFuBack.Models.Users;
using HomeFuBack.Data.DTO;
using Microsoft.Extensions.Configuration;
using HomeFuBack.Data.Entities;
using System.Security.Cryptography;
using HomeFuBack.Helpers.Interfaces;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;

    public AuthController(ApplicationDbContext context, IConfiguration configuration, IEmailSender emailSender)
    {
        _context = context;
        _configuration = configuration;
        _emailSender = emailSender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest("Email уже используется");
        }

        // 1) При регистрации указывал роль "USER"
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        user.Role = "User"; // Присваиваем роль "User" по умолчанию

        // Генерируем код подтверждения
        user.EmailConfirmCode = GenerateConfirmationCode(); // Генерируем код

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Отправка письма с кодом подтверждения
        try
        {
            var subject = "Подтверждение регистрации в HomeFu";
            var message = $"<h1>Добро пожаловать в HomeFu!</h1>" +
                          $"<p>Спасибо за регистрацию. Для подтверждения вашей почты, пожалуйста, используйте следующий код:</p>" +
                          $"<h2>{user.EmailConfirmCode}</h2>" +
                          $"<p>С уважением, команда HomeFu.</p>";

            // Если у вас есть URL вашего фронтенда, вы можете добавить сюда ссылку
            // Например, убедитесь, что в appsettings.json у вас есть "AppUrl"
            // "AppUrl": "http://localhost:3000" или ваш домен
            var appUrl = _configuration["AppUrl"];
            if (!string.IsNullOrEmpty(appUrl))
            {
                message += $"<p>Или перейдите по ссылке: <a href='{appUrl}/confirm-email?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(user.EmailConfirmCode)}'>Подтвердить Email</a></p>";
            }


            await _emailSender.SendEmailAsync(user.Email, subject, message);
            Console.WriteLine($"Код подтверждения email отправлен на {user.Email}");
        }
        catch (Exception ex)
        {
            // Логируем ошибку отправки письма, но не блокируем регистрацию.
            // В реальном приложении можно добавить запись в лог или другой механизм уведомления.
            Console.WriteLine($"Ошибка при отправке письма подтверждения на {user.Email}: {ex.Message}");
        }

        return Ok("Регистрация успешна. На вашу почту отправлен код подтверждения.");
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == confirmDto.Email);

        if (user == null)
        {
            return NotFound("Пользователь не найден.");
        }

        // Проверяем, что код подтверждения не пустой/null (то есть, email еще не подтвержден)
        // и что предоставленный код совпадает.
        if (string.IsNullOrEmpty(user.EmailConfirmCode) || user.EmailConfirmCode != confirmDto.ConfirmCode)
        {
            // Если код пустой, это означает, что email уже был подтвержден.
            if (string.IsNullOrEmpty(user.EmailConfirmCode))
            {
                return BadRequest("Email уже подтвержден.");
            }
            return BadRequest("Неверный код подтверждения.");
        }

        // Email подтвержден: очищаем код.
        user.EmailConfirmCode = null; // Устанавливаем в null или string.Empty
                                      // (в зависимости от того, как вы хотите трактовать "пусто")

        _context.Users.Update(user); // EF Core отследит изменения, но явное Update не повредит
        await _context.SaveChangesAsync();

        return Ok("Email успешно подтвержден.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLogin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLogin.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(userLogin.Password, user.Password))
        {
            return Unauthorized("Неверный email или пароль");
        }

        // Проверяем, подтвержден ли email (код подтверждения должен быть пустым/null)
        if (!string.IsNullOrEmpty(user.EmailConfirmCode))
        {
            return Unauthorized("Пожалуйста, подтвердите ваш email, используя код, отправленный на вашу почту.");
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        var accessTokenExpiryTime = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:ExpirationMinutes"]));
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        var tokenEntity = new Token
        {
            UserId = user.Id,
            SubmitDt = DateTime.UtcNow,
            ExpireDt = accessTokenExpiryTime,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpireDt = refreshTokenExpiryTime
        };

        _context.Tokens.Add(tokenEntity);
        await _context.SaveChangesAsync();

        // 2) При авторизации возвращал роль юзера
        return Ok(new
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            Role = user.Role // Возвращаем роль пользователя
        });
    }

    private string GenerateJwtToken(User user)
    {
        var keyString = _configuration["Jwt:Key"];
        var keyBytes = Encoding.UTF8.GetBytes(keyString!);

        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:ExpirationMinutes"])),
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    private string GenerateConfirmationCode()
    {
        // Генерируем 6-значный цифровой код
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
}