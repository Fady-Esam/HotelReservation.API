using HotelReservation.API.Domain.Enums;

namespace HotelReservation.API.API.Dtos.RoomDtos
{
    public class CreateRoomDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RoomType RoomType { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
    }
}
