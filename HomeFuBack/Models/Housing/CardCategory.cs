namespace HomeFuBack.Models.Housing
{
    public class CardCategory
    {
        public int CardId { get; set; }
        public Card Card { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
