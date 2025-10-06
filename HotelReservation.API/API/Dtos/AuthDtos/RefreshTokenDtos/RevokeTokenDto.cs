using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelReservation.API.API.Dtos.AuthDtos.RefreshTokenDtos
{
    public class RevokeRefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
    }
}
