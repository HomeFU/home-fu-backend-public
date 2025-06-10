using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using HomeFuBack.Models;

namespace HomeFuBack.Data.DTO
{
    // DTO для создания новой CardDetail (которая также создаст Card и Rating)
    public class CardDetailCreateDto
    {
        // --- Поля для CardDetail ---
        [Required(ErrorMessage = "Количество гостей обязательно.")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество гостей должно быть не менее 1.")]
        public int NumberOfGuests { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество спален должно быть неотрицательным.")]
        public int NumberOfBedrooms { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество кроватей должно быть неотрицательным.")]
        public int NumberOfBeds { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество ванных комнат должно быть неотрицательным.")]
        public int NumberOfBathrooms { get; set; }

        [Required(ErrorMessage = "ID хоста обязательно.")]
        public Guid HostId { get; set; }

        [Required(ErrorMessage = "Описание обязательно.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 2000 символов.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Широта обязательна.")]
        [Range(-90.0, 90.0, ErrorMessage = "Широта должна быть в диапазоне от -90 до 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Долгота обязательна.")]
        [Range(-180.0, 180.0, ErrorMessage = "Долгота должна быть в диапазоне от -180 до 180.")]
        public double Longitude { get; set; }

        public List<int>? AmenityIds { get; set; } // ID удобств

        // --- Поля для связанной Card ---
        [Required(ErrorMessage = "Название карточки обязательно.")]
        [StringLength(200, ErrorMessage = "Название карточки не может превышать 200 символов.")]
        public string CardName { get; set; }

        [Required(ErrorMessage = "ID локации обязательно.")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Дата начала доступности обязательна.")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 5, ErrorMessage = "Рейтинг должен быть от 0 до 5.")]
        public int? Rating { get; set; } // Средний рейтинг карточки

        public List<IFormFile>? CardImages { get; set; } // Файлы изображений для карточки

        [Required(ErrorMessage = "Цена обязательна.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше нуля.")]
        public decimal Price { get; set; }

        // --- Поля для создания связанного Rating (начальные значения) ---
        [Range(0.0, 5.0)]
        public double InitialCleanliness { get; set; } = 0.0;
        [Range(0.0, 5.0)]
        public double InitialAccuracy { get; set; } = 0.0;
        [Range(0.0, 5.0)]
        public double InitialCheckIn { get; set; } = 0.0;
        [Range(0.0, 5.0)]
        public double InitialCommunication { get; set; } = 0.0;
        [Range(0.0, 5.0)]
        public double InitialLocationRating { get; set; } = 0.0;
        [Range(0.0, 5.0)]
        public double InitialValue { get; set; } = 0.0;
    }

    // DTO для обновления существующей CardDetail
    public class CardDetailUpdateDto
    {
        // --- Поля для CardDetail ---
        [Range(1, int.MaxValue, ErrorMessage = "Количество гостей должно быть не менее 1.")]
        public int? NumberOfGuests { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество спален должно быть неотрицательным.")]
        public int? NumberOfBedrooms { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество кроватей должно быть неотрицательным.")]
        public int? NumberOfBeds { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество ванных комнат должно быть неотрицательным.")]
        public int? NumberOfBathrooms { get; set; }

        public Guid? HostId { get; set; } // HostId можно обновлять

        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 2000 символов.")]
        public string? Description { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Широта должна быть в диапазоне от -90 до 90.")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Долгота должна быть в диапазоне от -180 до 180.")]
        public double? Longitude { get; set; }

        public List<int>? AmenityIds { get; set; } // ID удобств, которые должны быть привязаны
        public List<int>? AmenitiesToRemove { get; set; } // ID удобств для удаления из списка

        // --- Поля для связанной Card ---
        [StringLength(200, ErrorMessage = "Название карточки не может превышать 200 символов.")]
        public string? CardName { get; set; }

        public int? LocationId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 5, ErrorMessage = "Рейтинг должен быть от 0 до 5.")]
        public int? Rating { get; set; }

        public List<IFormFile>? CardImages { get; set; } // Новые изображения для добавления
        public List<string>? ImageUrlsToRemove { get; set; } // URLы изображений для удаления

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше нуля.")]
        public decimal? Price { get; set; }

        public bool? IsDeleted { get; set; } // Флаг для мягкого удаления карточки

        // --- Поля для связанного Rating (для обновления) ---
        // Если обновлять через этот контроллер, то обновляется существующий Rating,
        // а не создается новый ID
        [Range(0.0, 5.0)]
        public double? Cleanliness { get; set; }
        [Range(0.0, 5.0)]
        public double? Accuracy { get; set; }
        [Range(0.0, 5.0)]
        public double? CheckIn { get; set; }
        [Range(0.0, 5.0)]
        public double? Communication { get; set; }
        [Range(0.0, 5.0)]
        public double? LocationRating { get; set; }
        [Range(0.0, 5.0)]
        public double? Value { get; set; }
    }

    // DTO для ответа GET запросов
    public class CardDetailResponseDto
    {
        public int Id { get; set; }
        public int NumberOfGuests { get; set; }
        public int NumberOfBedrooms { get; set; }
        public int NumberOfBeds { get; set; }
        public int NumberOfBathrooms { get; set; }
        public Guid HostId { get; set; }
        public string HostName { get; set; } // Имя хоста из User
        public string? HostAvatarUrl { get; set; } // Аватар хоста из User (если есть)
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public List<AmenityResponseDto> Amenities { get; set; } // Список удобств с их данными

        public RatingDto? Ratings { get; set; } // Вложенный DTO для оценок

        public CardResponseDto? Card { get; set; } // Вложенный DTO для основной карточки
    }

    // DTO для Amenity, чтобы его можно было использовать внутри CardDetailResponseDto
    public class AmenityResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
    }

}