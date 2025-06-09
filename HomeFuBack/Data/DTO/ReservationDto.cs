using System;
using System.ComponentModel.DataAnnotations;
using HomeFuBack.Models.Housing;

namespace HomeFuBack.Data.DTO
{
    public class ReservationDto
    {
        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int Adults { get; set; }

        public int Children { get; set; }

        public int Infants { get; set; }

        public int Pets { get; set; }

        [Required]
        public int CardId { get; set; }
    }

    public class ReservationUpdateDto
    {
        // Убрал Id, CardId, UserId - обычно их не обновляют
        // public int? Id { get; set; } // Если у вас было это
        // public int? CardId { get; set; }
        // public Guid? UserId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckOutDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Количество взрослых должно быть не менее 1.")]
        public int? Adults { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество детей должно быть неотрицательным.")]
        public int? Children { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество младенцев должно быть неотрицательным.")]
        public int? Infants { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество питомцев должно быть неотрицательным.")]
        public int? Pets { get; set; }

        // Если вы хотите, чтобы фронтенд мог отправлять "Pending", "Confirmed" и т.д.,
        // то Status должен быть string?, а не ReservationStatus?
        // Затем в контроллере парсим string в enum.
        public ReservationStatus? Status { get; set; } // Если фронтенд отправляет числовые значения enum
        // public string? Status { get; set; } // Если фронтенд отправляет строковые значения enum
    }

    public class ReservationResponseDto
    {
        public int Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public int Pets { get; set; }

        public int CardId { get; set; }
        public string? CardName { get; set; } // Название карточки
        public List<string>? CardImageUrls { get; set; } // URLы изображений

        public Guid UserId { get; set; }
        public string? UserName { get; set; } // Имя пользователя
        public string? UserEmail { get; set; } // Email пользователя

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // Статус как строка
    }
}