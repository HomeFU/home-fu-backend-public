namespace HomeFuBack.Data.DTO
{
    public class CategoryDto
    {
        public string Name { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public class CategoryUpdateDto
    {
        public string? Name { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}