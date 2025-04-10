using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<CardCategory> CardCategories { get; set; }
    }
}
