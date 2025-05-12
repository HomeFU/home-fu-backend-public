namespace HomeFuBack.Data.DTO
{
    public class CardResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } // Или объект LocationDto
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Rating { get; set; }
        public decimal Price { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> ImageUrls { get; set; }

        public List<int> CategoryIds { get; set; }
    }
}