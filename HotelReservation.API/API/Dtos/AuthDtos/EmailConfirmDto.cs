using System.ComponentModel.DataAnnotations;

namespace HotelReservation.API.API.Dtos.AuthDtos
{
    public class EmailConfirmDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
