using System;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

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
        public Card? Card { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    }

    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}
