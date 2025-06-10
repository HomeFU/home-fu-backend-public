namespace HomeFuBack.Data.DTO
{
    public class CardUpdateDto
    {
        public int Id { get; set; } // Обязательно, чтобы связать с URL
        public string? Name { get; set; }
        public int? LocationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Price { get; set; }
        public bool? IsDeleted { get; set; }
        public List<string>? ImageUrls { get; set; } // Если у вас есть это поле в CardDto
        public List<int>? CategoryIds { get; set; } // Только те ID категорий, которые вы хотите установить
    }
}
