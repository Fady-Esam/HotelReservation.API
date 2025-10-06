
using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Enums;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.API.DL.Repositories
{
    public class UserConfirmationCodeRepository : GenericRepository<UserConfirmationCode>, IUserConfirmationCodeRepository 
    {
        public UserConfirmationCodeRepository(ApplicationDbContext context, ILogger<UserConfirmationCodeRepository> logger)
    :    base(context, logger)
        {
        }

        public async Task<UserConfirmationCode?> GetLatestAsync(string userId, ConfirmationType type, string code)
        {
            _logger.LogInformation("Searching for latest confirmation code of type {ConfirmationType} for user {UserId}", type, userId);

            var confirmationCode = await _context.UserConfirmationCodes
                .Where(c => c.UserId == userId && c.Type == type && c.Code == code && !c.IsUsed)
                .OrderByDescending(c => c.ExpireAt)
                .FirstOrDefaultAsync();

            if (confirmationCode == null)
            {
                _logger.LogWarning("No valid, unused confirmation code of type {ConfirmationType} found for user {UserId}", type, userId);
            }

            return confirmationCode;
        }
    }

}
