using System.ComponentModel.DataAnnotations;

namespace HomeFuBack.Data.DTO
{
    public class RatingDto
    {
        [Range(0.0, 5.0)]
        public double Cleanliness { get; set; }

        [Range(0.0, 5.0)]
        public double Accuracy { get; set; }

        [Range(0.0, 5.0)]
        public double CheckIn { get; set; }

        [Range(0.0, 5.0)]
        public double Communication { get; set; }

        [Range(0.0, 5.0)]
        public double Location { get; set; }

        [Range(0.0, 5.0)]
        public double Value { get; set; }
    }
}
