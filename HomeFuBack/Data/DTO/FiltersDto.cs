using System;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class FiltersDto
    {
        [DataType(DataType.Date)]
        public DateTime? CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckOutDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество взрослых должно быть неотрицательным.")]
        public int? Adults { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество детей должно быть неотрицательным.")]
        public int? Children { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество младенцев должно быть неотрицательным.")]
        public int? Infants { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество питомцев должно быть неотрицательным.")]
        public int? Pets { get; set; }

        public int? LocationId { get; set; }
        public string? SearchTerm { get; set; }
    }
}