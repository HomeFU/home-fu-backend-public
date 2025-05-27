namespace HomeFuBack.Models.Housing
{
    public class CardDetailAmenity
    {
        public int CardDetailId { get; set; }
        public CardDetail CardDetail { get; set; }

        public int AmenityId { get; set; }
        public Amenity Amenity { get; set; }
    }
}
