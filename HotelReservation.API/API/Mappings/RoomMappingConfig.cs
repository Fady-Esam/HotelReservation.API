using AutoMapper;
using Clinic.API.API.Dtos.ApplicationUserDtos;
using HotelReservation.API.API.Dtos.AuthDtos;
using HotelReservation.API.API.Dtos.RoomDtos;
using HotelReservation.API.Domain.Entities;

namespace HotelReservation.API.API.Mappings
{
    public class RoomMappingConfig
    {
        public void Configure(Profile profile)
        {
            profile.CreateMap<Room, RoomDto>()
                .ForMember(dest => dest.RoomType,
                           opt => opt.MapFrom(src => src.RoomType.ToString()));
            profile.CreateMap<CreateRoomDto, Room>()
                .ForMember(dest => dest.CreatedAt,
                           opt => opt.MapFrom(_ => DateTime.UtcNow));

            profile.CreateMap<UpdateRoomDto, Room>()
                .ForMember(dest => dest.UpdatedAt,
                           opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // This ignores null values during update
        


        }
    }
}
