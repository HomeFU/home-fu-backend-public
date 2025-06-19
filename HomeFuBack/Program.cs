using HomeFuBack.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;
using HomeFuBack.Models.Users;
using HomeFuBack.Helpers;
using HomeFuBack.Helpers.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Добавляем строку подключения к базе данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    options => options.EnableRetryOnFailure()
    ));

// Разрешаем CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Добавляем контроллеры
builder.Services.AddControllers();

// 1. Привязываем секцию "EmailSettings" из appsettings.json к классу EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 2. Регистрируем IEmailSender и его реализацию EmailSender в контейнере зависимостей
// AddScoped означает, что новый экземпляр EmailSender будет создаваться для каждого HTTP-запроса.
// Это хороший выбор для большинства сервисов.
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Включаем Swagger для тестирования API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.MaxDepth = 32;
});

var app = builder.Build();

// Конфигурация middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".avif"] = "image/avif",
    [".webp"] = "image/webp",
    [".jpg"] = "image/jpg",
    [".jpeg"] = "image/jpeg",
    [".png"] = "image/png",
    [".svg"] = "image/svg+xml",

});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "images")),
    RequestPath = "/images",
    ContentTypeProvider = provider // Используем FileExtensionContentTypeProvider
});

app.UseHttpsRedirection();

// Включаем CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();