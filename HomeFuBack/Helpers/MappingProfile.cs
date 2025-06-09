using AutoMapper;
using HomeFuBack.Models; // Убедитесь, что это пространство имен содержит Reservation, User, CardDetail, Card
using HomeFuBack.Models.Housing; // Убедитесь, что это пространство имен содержит Card, Location, CardCategory
using HomeFuBack.Data.DTO; // Ваши DTO

namespace HomeFuBack.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Маппинг для Reservation
            CreateMap<ReservationDto, Reservation>();
            CreateMap<ReservationUpdateDto, Reservation>();


            // Если у вас есть другие DTO и модели, убедитесь, что они тоже корректно маппятся
            // Например, для CardDetailResponseDto:
            CreateMap<CardDetail, CardDetailResponseDto>()
                 .ForMember(dest => dest.HostName, opt => opt.MapFrom(src => src.Host != null ? src.Host.Email : null))
                 .ForMember(dest => dest.HostAvatarUrl, opt => opt.MapFrom(src => src.Host != null ? src.Host.ProfileImageUrl : null)) // Убедитесь, что у вашей модели User есть AvatarUrl
                 .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.CardDetailAmenities.Select(cda => cda.Amenity))); // Маппинг удобств

            // Если вы используете AmenityResponseDto:
            CreateMap<Amenity, AmenityResponseDto>();

            // Если вы используете RatingDto:
            CreateMap<Rating, RatingDto>()
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.OverallRating)); // Устранение расхождения в названиях полей
                                                                                                  // (LocationRating в модели, Location в DTO)
                                                                                                  // Маппинг для CardResponseDto (если он используется где-либо еще, например, в CardDetailResponseDto)
            CreateMap<Card, CardResponseDto>()
                 .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
                 .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ImageUrls)) // Теперь это прямое маппинг List<string>
                 .ForMember(dest => dest.CategoryIds, opt => opt.MapFrom(src => src.CardCategories.Select(cc => cc.CategoryId).ToList()));


            // Важно: Убедитесь, что ваши модели User и Amenity также маппятся, если они используются в DTO
            // Пример для User, если вы хотите иметь UserResponseDto
            // CreateMap<User, UserResponseDto>(); // Если вы определили такой DTO
        }
    }
}