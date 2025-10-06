using HotelReservation.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HotelReservation.API.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApplicationUser?> FindUserByIdAsync(string userId);
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<ApplicationUser?> FindUserByUserNameAsync(string userName);
        Task<ApplicationUser?> FindUserByPhoneNumberAsync(string phoneNumber);
        Task<ApplicationUser?> FindUserByLoginAsync(string loginProvider, string providerKey);
        Task<bool> CheckUserNameExistsAsync(string userName);
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> CheckPhoneNumberExistsAsync(string phoneNumber);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

        Task<(IdentityResult, string)> CreateUserAsync(ApplicationUser user, string? password = null);
        Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo loginInfo);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);

        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<IList<Claim>> GetUserClaimsAsync(ApplicationUser user);

        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);

        Task SetExternalAuthTokenAsync(ApplicationUser user, string loginProvider, string tokenName, string tokenValue);
        Task RemoveExternalAuthTokenAsync(ApplicationUser user, string loginProvider, string tokenName);

        Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task UpdateRefreshToken(RefreshToken refreshToken);
    }
}
