using AutoMapper;
using Google.Apis.Auth;
using HotelReservation.API.API.Dtos.AuthDtos;
using HotelReservation.API.API.Dtos.AuthDtos.FacebookDtos;
using HotelReservation.API.API.Dtos.AuthDtos.PasswordDtos;
using HotelReservation.API.API.Dtos.AuthDtos.RefreshTokenDtos;
using HotelReservation.API.BL.Interfaces;
using HotelReservation.API.Common.Responses;
using HotelReservation.API.Common.Settings;
using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Enums;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HotelReservation.API.BL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo; // Replaced UserManager and IRefreshTokenRepository
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IUserConfirmationCodeService _confirmationCodeService;
        private readonly IMapper _mapper;
        private readonly JwtSetting _jwtSetting;
        private readonly GoogleSetting _googleSetting;
        private readonly FacebookSetting _facebookSetting;

        public AuthService(
            IAuthRepository authRepo,
            IEmailService emailService,
            ISmsService smsService,
            IUserConfirmationCodeService confirmationCodeService,
            IMapper mapper,
            JwtSetting jwtSetting,
            GoogleSetting googleSetting,
            FacebookSetting facebookSetting)
        {
            _authRepo = authRepo;
            _emailService = emailService;
            _smsService = smsService;
            _confirmationCodeService = confirmationCodeService;
            _mapper = mapper;
            _jwtSetting = jwtSetting;
            _googleSetting = googleSetting;
            _facebookSetting = facebookSetting;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            const string errMessage = "Registration failed";

            if (!string.IsNullOrWhiteSpace(dto.UserName) && await _authRepo.CheckUserNameExistsAsync(dto.UserName))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "User name is already in use by another user" });

            if (!string.IsNullOrWhiteSpace(dto.Email) && await _authRepo.CheckEmailExistsAsync(dto.Email))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Email is already in use by another user" });

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && await _authRepo.CheckPhoneNumberExistsAsync(dto.PhoneNumber))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Phone number is already in use by another user" });

            var user = _mapper.Map<ApplicationUser>(dto);

            var (result, userId) = await _authRepo.CreateUserAsync(user, dto.Password);
            if (!result.Succeeded)
                return ApiResponse<AuthResponseDto>.Failure(errMessage, result.Errors.Select(e => e.Description).ToList());

            user.Id = userId; // Ensure user object has the ID for subsequent operations

            if (!string.IsNullOrEmpty(user.Email))
                await _emailService.SendConfirmationCodeAsync(user.Email, await _confirmationCodeService.GenerateCodeAsync(user.Id, ConfirmationType.Email));

            if (!string.IsNullOrEmpty(user.PhoneNumber))
                await _smsService.SendConfirmationCodeAsync(user.PhoneNumber, await _confirmationCodeService.GenerateCodeAsync(user.Id, ConfirmationType.SMS));

            var authResponse = await CreateAuthenticatedResponse(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "User registered successfully", StatusCodes.Status201Created);
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            const string errMessage = "Login failed";
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(dto.UserName))
                user = await _authRepo.FindUserByUserNameAsync(dto.UserName);
            else if (!string.IsNullOrWhiteSpace(dto.Email))
                user = await _authRepo.FindUserByEmailAsync(dto.Email);
            else if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                user = await _authRepo.FindUserByPhoneNumberAsync(dto.PhoneNumber);

            if (user == null || !await _authRepo.CheckPasswordAsync(user, dto.Password))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Invalid credentials" }, StatusCodes.Status401Unauthorized);

            if (user.Email != null && !user.EmailConfirmed && !string.IsNullOrWhiteSpace(dto.Email))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Email is not confirmed" }, StatusCodes.Status403Forbidden);

            if (user.PhoneNumber != null && !user.PhoneNumberConfirmed && !string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Phone Number is not confirmed" }, StatusCodes.Status403Forbidden);

            var authResponse = await CreateAuthenticatedResponse(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSetting.ClientId }
                });
            }
            catch
            {
                return ApiResponse<AuthResponseDto>.Failure("Failed to login with Google", new() { "Invalid Google token" });
            }

            var user = await _authRepo.FindUserByLoginAsync("Google", payload.Subject);
            if (user == null)
            {
                user = await _authRepo.FindUserByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = payload.Name, Email = payload.Email, EmailConfirmed = true };
                    var (createResult, _) = await _authRepo.CreateUserAsync(user);
                    if (!createResult.Succeeded)
                        return ApiResponse<AuthResponseDto>.Failure("Failed to create user", createResult.Errors.Select(e => e.Description).ToList());
                }

                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                var addLoginResult = await _authRepo.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                    return ApiResponse<AuthResponseDto>.Failure("Failed to add Google login", addLoginResult.Errors.Select(e => e.Description).ToList());
            }

            await _authRepo.SetExternalAuthTokenAsync(user, "Google", "id_token", idToken);

            var authResponse = await CreateAuthenticatedResponse(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> FacebookLoginAsync(string accessToken)
        {
            const string errMessage = "Failed to login with Facebook";

            using var httpClient = new HttpClient();
            var appAccessToken = $"{_facebookSetting.AppId}|{_facebookSetting.AppSecret}";
            var debugTokenResponse = await httpClient.GetFromJsonAsync<FacebookDebugTokenResponseDto>(
                $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}");

            if (debugTokenResponse?.Data?.IsValid != true)
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Invalid Facebook token" });

            var userResponse = await httpClient.GetFromJsonAsync<FacebookUserResponseDto>(
                $"https://graph.facebook.com/me?fields=id,email,name&access_token={accessToken}");

            if (userResponse == null || string.IsNullOrEmpty(userResponse.Id))
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Could not fetch user info from Facebook" });

            var user = await _authRepo.FindUserByLoginAsync("Facebook", userResponse.Id);
            if (user == null)
            {
                user = await _authRepo.FindUserByEmailAsync(userResponse.Email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = userResponse.Name, Email = userResponse.Email, EmailConfirmed = true };
                    var (createResult, _) = await _authRepo.CreateUserAsync(user);
                    if (!createResult.Succeeded)
                        return ApiResponse<AuthResponseDto>.Failure("Failed to create user", createResult.Errors.Select(e => e.Description).ToList());
                }

                var loginInfo = new UserLoginInfo("Facebook", userResponse.Id, "Facebook");
                var addLoginResult = await _authRepo.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                    return ApiResponse<AuthResponseDto>.Failure("Failed to add Facebook login", addLoginResult.Errors.Select(e => e.Description).ToList());
            }

            await _authRepo.SetExternalAuthTokenAsync(user, "Facebook", "access_token", accessToken);

            var authResponse = await CreateAuthenticatedResponse(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(CreateNewRefreshTokenDto dto)
        {
            const string errMessage = "Token refresh failed";

            var storedToken = await _authRepo.GetRefreshTokenByTokenAsync(dto.Token);
            if (storedToken == null)
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Refresh token not found" }, StatusCodes.Status404NotFound);

            if (!storedToken.IsActive)
            {
                return storedToken.ReplacedByToken != null
                    ? ApiResponse<AuthResponseDto>.Failure("Token reuse detected. Please login again.", new() { "This refresh token has already been rotated" }, StatusCodes.Status401Unauthorized)
                    : ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "Refresh token is not active" }, StatusCodes.Status401Unauthorized);
            }

            var user = await _authRepo.FindUserByIdAsync(storedToken.UserId);
            if (user == null)
                return ApiResponse<AuthResponseDto>.Failure(errMessage, new() { "User not found" }, StatusCodes.Status404NotFound);

            var newRefreshTokenGenerated = GenerateRefreshToken();
            storedToken.Revoked = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshTokenGenerated;
            await _authRepo.UpdateRefreshToken(storedToken);

            var newRefreshToken = new RefreshToken { UserId = user.Id, Token = newRefreshTokenGenerated };
            await _authRepo.AddRefreshTokenAsync(newRefreshToken);

            var authResponse = await CreateAuthenticatedResponse(user, newRefreshToken);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Token refreshed successfully");
        }

        public async Task<ApiResponse<object>> RevokeRefreshTokenAsync(RevokeRefreshTokenDto dto)
        {
            var storedToken = await _authRepo.GetRefreshTokenByTokenAsync(dto.Token);
            if (storedToken == null)
                return ApiResponse<object>.Failure("Failed to revoke refresh token", new() { "Token not found" }, StatusCodes.Status404NotFound);

            var user = await _authRepo.FindUserByIdAsync(storedToken.UserId);
            if (user == null)
                return ApiResponse<object>.Failure("Failed to revoke refresh token", new() { "User not found" }, StatusCodes.Status404NotFound);

            _mapper.Map(dto, storedToken);
            await _authRepo.UpdateRefreshToken(storedToken);

            await _authRepo.RemoveExternalAuthTokenAsync(user, "Google", "id_token");
            await _authRepo.RemoveExternalAuthTokenAsync(user, "Facebook", "access_token");

            return ApiResponse<object>.SuccessNoData("Refresh Token Revoked Successfully");
        }

        public async Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordDto dto)
        {
            const string errMessage = "Failed to change password";
            var user = await _authRepo.FindUserByIdAsync(dto.UserId);
            if (user == null)
                return ApiResponse<object>.Failure(errMessage, new() { "User not found" }, StatusCodes.Status404NotFound);

            var result = await _authRepo.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return ApiResponse<object>.Failure(errMessage, result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<object>.SuccessNoData("Password changed successfully");
        }

        public async Task<ApiResponse<object>> RequestPasswordResetAsync(PasswordResetRequestDto dto)
        {
            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(dto.Email))
                user = await _authRepo.FindUserByEmailAsync(dto.Email);
            else if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                user = await _authRepo.FindUserByPhoneNumberAsync(dto.PhoneNumber);

            if (user == null)
                return ApiResponse<object>.Failure("Failed to reset password", new() { "User not found" }, StatusCodes.Status404NotFound);

            var code = await _confirmationCodeService.GenerateCodeAsync(user.Id, ConfirmationType.PasswordReset);

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                await _emailService.SendPasswordResetCodeAsync(user.Email!, code);
                return ApiResponse<object>.SuccessNoData("Password reset code sent to email");
            }

            await _smsService.SendPasswordResetCodeAsync(user.PhoneNumber!, code);
            return ApiResponse<object>.SuccessNoData("Password reset code sent to phone number");
        }

        public async Task<ApiResponse<object>> ConfirmPasswordResetAsync(PasswordResetConfirmDto dto)
        {
            const string errMessage = "Failed to confirm reset password";
            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(dto.Email))
                user = await _authRepo.FindUserByEmailAsync(dto.Email);
            else if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                user = await _authRepo.FindUserByPhoneNumberAsync(dto.PhoneNumber);

            if (user == null)
                return ApiResponse<object>.Failure(errMessage, new() { "User not found" }, StatusCodes.Status404NotFound);

            var isValid = await _confirmationCodeService.ValidateCodeAsync(user.Id, dto.Code, ConfirmationType.PasswordReset);
            if (!isValid)
                return ApiResponse<object>.Failure(errMessage, new() { "Invalid or expired reset code" }, StatusCodes.Status400BadRequest);

            var resetToken = await _authRepo.GeneratePasswordResetTokenAsync(user);
            var result = await _authRepo.ResetPasswordAsync(user, resetToken, dto.NewPassword);
            if (!result.Succeeded)
                return ApiResponse<object>.Failure(errMessage, result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<object>.SuccessNoData("Password reset successfully");
        }

        public async Task<ApiResponse<object>> ConfirmEmailAsync(EmailConfirmDto dto)
        {
            var user = await _authRepo.FindUserByEmailAsync(dto.Email);
            if (user == null)
                return ApiResponse<object>.Failure("Failed to confirm Email", new() { "Email not registered" }, StatusCodes.Status404NotFound);

            if (!await _confirmationCodeService.ValidateCodeAsync(user.Id, dto.Code, ConfirmationType.Email))
                return ApiResponse<object>.Failure("Failed to confirm Email", new() { "Invalid Confirmation Code" });

            user.EmailConfirmed = true;
            await _authRepo.UpdateUserAsync(user);
            return ApiResponse<object>.SuccessNoData("Email confirmed successfully");
        }

        public async Task<ApiResponse<object>> ConfirmSmsAsync(SmsConfirmDto dto)
        {
            var user = await _authRepo.FindUserByPhoneNumberAsync(dto.PhoneNumber);
            if (user == null)
                return ApiResponse<object>.Failure("Failed to confirm Phone Number", new() { "Phone number not registered" }, StatusCodes.Status404NotFound);

            if (!await _confirmationCodeService.ValidateCodeAsync(user.Id, dto.Code, ConfirmationType.SMS))
                return ApiResponse<object>.Failure("Failed to confirm Phone Number", new() { "Invalid Confirmation Code" });

            user.PhoneNumberConfirmed = true;
            await _authRepo.UpdateUserAsync(user);
            return ApiResponse<object>.SuccessNoData("Phone number confirmed successfully");
        }

        // 🔹 Helpers
        private async Task<AuthResponseDto> CreateAuthenticatedResponse(ApplicationUser user, RefreshToken? refreshToken = null)
        {
            var accessToken = await GenerateJwtToken(user);

            if (refreshToken == null)
            {
                refreshToken = new RefreshToken { UserId = user.Id, Token = GenerateRefreshToken() };
                await _authRepo.AddRefreshTokenAsync(refreshToken);
            }

            return new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
                RefreshToken = refreshToken.Token,
                Roles = (await _authRepo.GetUserRolesAsync(user)).ToList(),
                Claims = accessToken.Claims.ToList(),
                AccessTokenExpiration = accessToken.ValidTo,
                RefreshTokenExpiration = refreshToken.Expires
            };
        }

        private async Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            };

            var roles = await _authRepo.GetUserRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(await _authRepo.GetUserClaimsAsync(user));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSetting.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSetting.ExpirationInMinutes);

            return new JwtSecurityToken(
                issuer: _jwtSetting.Issuer,
                audience: _jwtSetting.Audience,
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: credentials
            );
        }

        private static string GenerateRefreshToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
