using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Models.Housing
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<Card> Cards { get; set; }
    }
}
