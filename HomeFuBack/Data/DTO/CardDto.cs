using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class CardDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int LocationId { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public bool IsDeleted { get; set; }

        public List<IFormFile> Images { get; set; }

        public List<int> CategoryIds { get; set; }
    }
}
