using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.API.DL.Repositories
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            _logger.LogInformation("Attempting to find refresh token by its value.");
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null)
            {
                _logger.LogWarning("A refresh token with the specified value was not found.");
            }

            return refreshToken;
        }
    }
}
