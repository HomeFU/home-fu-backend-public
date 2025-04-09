using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HomeFuBack.Data;
using HomeFuBack.Data.Entities;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using HomeFuBack.Helpers;

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

		var token = GenerateJwtToken(user);

		return Ok(new { Token = token });
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
}

public class UserLoginDto
{
	public string Email { get; set; }
	public string Password { get; set; }
}