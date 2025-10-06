

using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Enums;

namespace HotelReservation.API.Domain.Interfaces
{
    public interface IUserConfirmationCodeRepository : IGenericRepository<UserConfirmationCode>
    {
        Task<UserConfirmationCode?> GetLatestAsync(string userId, ConfirmationType type, string code);
    }
}
