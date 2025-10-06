using HotelReservation.API.BL.Interfaces;
using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Enums;
using HotelReservation.API.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace HotelReservation.API.BL.Services
{
    public class UserConfirmationCodeService : IUserConfirmationCodeService
    {
        private readonly IUserConfirmationCodeRepository _repository;

        private const int CodeLength = 6;
        private readonly TimeSpan ExpiryDuration = TimeSpan.FromMinutes(2);

        public UserConfirmationCodeService(IUserConfirmationCodeRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> GenerateCodeAsync(string userId, ConfirmationType type)
        {
            var code = GenerateSecureCode(CodeLength);

            var confirmation = new UserConfirmationCode
            {
                UserId = userId,
                Code = code,
                Type = type,
                ExpireAt = DateTime.UtcNow.Add(ExpiryDuration)
            };

            await _repository.AddAsync(confirmation).ConfigureAwait(false);
            return code;
        }

        public async Task<bool> ValidateCodeAsync(string userId, string code, ConfirmationType type)
        {
            var confirmation = await _repository.GetLatestAsync(userId, type, code).ConfigureAwait(false);

            if (confirmation == null || confirmation.ExpireAt < DateTime.UtcNow)
                return false;

            confirmation.IsUsed = true;
            await _repository.AddAsync(confirmation).ConfigureAwait(false);

            return true;
        }

        private static string GenerateSecureCode(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            var sb = new StringBuilder(length);
            foreach (var b in bytes)
                sb.Append((b % 10).ToString());

            return sb.ToString()[..length];
        }
    }

}
