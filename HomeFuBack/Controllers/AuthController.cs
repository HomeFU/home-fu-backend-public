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
using HomeFuBack.Helpers;
using HomeFuBack.Models.Users;
using HomeFuBack.Data.DTO;
using Microsoft.Extensions.Configuration;
using HomeFuBack.Data.Entities;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest("Email уже используется");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("Регистрация успешна");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLogin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLogin.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(userLogin.Password, user.Password))
        {
            return Unauthorized("Неверный email или пароль");
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

        return Ok(new { Token = accessToken, RefreshToken = refreshToken });
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
}