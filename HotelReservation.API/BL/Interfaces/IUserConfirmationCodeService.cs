
using HotelReservation.API.Domain.Enums;

namespace HotelReservation.API.BL.Interfaces
{
    public interface IUserConfirmationCodeService
    {
        Task<string> GenerateCodeAsync(string userId, ConfirmationType type);
        Task<bool> ValidateCodeAsync(string userId, string code, ConfirmationType type);
    }
}
