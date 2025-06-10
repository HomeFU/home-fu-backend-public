using AutoMapper;
using HomeFuBack.Models.Housing;
using HomeFuBack.Data.DTO;

namespace HomeFuBack.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Маппинг для Reservation
            CreateMap<ReservationDto, Reservation>();
            CreateMap<ReservationUpdateDto, Reservation>();


            // CardDetailResponseDto:
            CreateMap<CardDetail, CardDetailResponseDto>()
                 .ForMember(dest => dest.HostName, opt => opt.MapFrom(src => src.Host != null ? src.Host.Email : null))
                 .ForMember(dest => dest.HostAvatarUrl, opt => opt.MapFrom(src => src.Host != null ? src.Host.ProfileImageUrl : null)) // Убедитесь, что у вашей модели User есть AvatarUrl
                 .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.CardDetailAmenities.Select(cda => cda.Amenity))); // Маппинг удобств

            // AmenityResponseDto:
            CreateMap<Amenity, AmenityResponseDto>();

            // RatingDto:
            CreateMap<Rating, RatingDto>()
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.OverallRating)); // Устранение расхождения в названиях полей
                                                                                                  // (LocationRating в модели, Location в DTO)

            CreateMap<Card, CardResponseDto>()
                 .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
                 .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ImageUrls)) // Прямой маппинг List<string>
                 .ForMember(dest => dest.CategoryIds, opt => opt.MapFrom(src => src.CardCategories.Select(cc => cc.CategoryId).ToList()));

        }
    }
}