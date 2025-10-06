using HotelReservation.API.Common.Models;
using HotelReservation.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservation.API.Domain.Entities
{
    public class Room : BaseEntity
    {
        [MaxLength(10)]
        public string RoomNumber { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [EnumDataType(typeof(RoomType))]
        public RoomType RoomType { get; set; }
        public int Capacity { get; set; } // How many people can stay

        [Column(TypeName = "decimal(9, 2)")]
        public decimal PricePerNight { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
