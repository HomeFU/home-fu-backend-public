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
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public int? Infants { get; set; }
        public int? Pets { get; set; }
        public ReservationStatus? Status { get; set; }
    }
}