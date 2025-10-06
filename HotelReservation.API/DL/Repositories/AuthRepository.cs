using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelReservation.API.DL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            IRefreshTokenRepository refreshTokenRepo,
            ILogger<AuthRepository> logger)
        {
            _userManager = userManager;
            _refreshTokenRepo = refreshTokenRepo;
            _logger = logger;
        }

        public async Task<ApplicationUser?> FindUserByIdAsync(string userId)
        {
            _logger.LogInformation("Attempting to find user by ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} was not found.", userId);
            }
            return user;
        }

        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            _logger.LogInformation("Attempting to find user by email.");
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} was not found.", email);
            }
            return user;
        }

        public async Task<ApplicationUser?> FindUserByUserNameAsync(string userName)
        {
            _logger.LogInformation("Attempting to find user by username: {UserName}", userName);
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                _logger.LogWarning("User with username {UserName} was not found.", userName);
            }
            return user;
        }

        public async Task<ApplicationUser?> FindUserByPhoneNumberAsync(string phoneNumber)
        {
            _logger.LogInformation("Attempting to find user by phone number.");
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null)
            {
                _logger.LogWarning("User with the specified phone number was not found.");
            }
            return user;
        }

        public async Task<ApplicationUser?> FindUserByLoginAsync(string loginProvider, string providerKey)
        {
            _logger.LogInformation("Attempting to find user by login provider: {LoginProvider}", loginProvider);
            // This query can be inefficient on large user bases. Consider alternative strategies if performance is an issue.
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var logins = await _userManager.GetLoginsAsync(user);
                if (logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
                {
                    _logger.LogInformation("Found user {UserId} for login provider {LoginProvider}", user.Id, loginProvider);
                    return user;
                }
            }
            _logger.LogWarning("No user found for login provider {LoginProvider} with the specified provider key.", loginProvider);
            return null;
        }

        public async Task<bool> CheckUserNameExistsAsync(string userName) =>
            await _userManager.Users.AsNoTracking().AnyAsync(u => u.NormalizedUserName == userName.ToUpper());

        public async Task<bool> CheckEmailExistsAsync(string email) =>
            await _userManager.Users.AsNoTracking().AnyAsync(u => u.NormalizedEmail == email.ToUpper());

        public async Task<bool> CheckPhoneNumberExistsAsync(string phoneNumber) =>
            await _userManager.Users.AsNoTracking().AnyAsync(u => u.PhoneNumber == phoneNumber);

        public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        {
            _logger.LogInformation("Checking password for user: {UserId}", user.Id);
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<(IdentityResult, string)> CreateUserAsync(ApplicationUser user, string? password = null)
        {
            _logger.LogInformation("Attempting to create user record for {UserName}", user.UserName);
            var result = string.IsNullOrEmpty(password)
                ? await _userManager.CreateAsync(user)
                : await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user {UserName}. Errors: {Errors}", user.UserName, errors);
            }
            else
            {
                _logger.LogInformation("Successfully created user {UserName} with ID {UserId}", user.UserName, user.Id);
            }
            return (result, user.Id);
        }

        public async Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo loginInfo)
        {
            _logger.LogInformation("Adding external login provider {LoginProvider} for user {UserId}", loginInfo.LoginProvider, user.Id);
            return await _userManager.AddLoginAsync(user, loginInfo);
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
        {
            _logger.LogInformation("Updating user {UserId}", user.Id);
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user) =>
            await _userManager.GetRolesAsync(user);

        public async Task<IList<Claim>> GetUserClaimsAsync(ApplicationUser user) =>
            await _userManager.GetClaimsAsync(user);

        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            _logger.LogInformation("Attempting to change password for user {UserId}", user.Id);
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to change password for user {UserId}. Errors: {Errors}", user.Id, errors);
            }
            return result;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            _logger.LogInformation("Generating password reset token for user {UserId}", user.Id);
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            _logger.LogInformation("Attempting to reset password for user {UserId}", user.Id);
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task SetExternalAuthTokenAsync(ApplicationUser user, string loginProvider, string tokenName, string tokenValue)
        {
            _logger.LogInformation("Setting external auth token {TokenName} for provider {LoginProvider} on user {UserId}", tokenName, loginProvider, user.Id);
            await _userManager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, tokenValue);
        }

        public async Task RemoveExternalAuthTokenAsync(ApplicationUser user, string loginProvider, string tokenName)
        {
            _logger.LogInformation("Removing external auth token {TokenName} for provider {LoginProvider} on user {UserId}", tokenName, loginProvider, user.Id);
            await _userManager.RemoveAuthenticationTokenAsync(user, loginProvider, tokenName);
        }

        public async Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token)
        {
            _logger.LogInformation("Calling RefreshTokenRepository to get token.");
            return await _refreshTokenRepo.GetByTokenAsync(token);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            _logger.LogInformation("Calling RefreshTokenRepository to add token for user {UserId}", refreshToken.UserId);
            await _refreshTokenRepo.AddAsync(refreshToken);
        }

        public Task UpdateRefreshToken(RefreshToken refreshToken)
        {
            _logger.LogInformation("Calling RefreshTokenRepository to update token ID {TokenId}", refreshToken.Id);
            _refreshTokenRepo.Update(refreshToken);
            return Task.CompletedTask;
        }
    }
}
