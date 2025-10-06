using FluentValidation;
using HotelReservation.API.API.Dtos.AuthDtos.RefreshTokenDtos;

namespace HotelReservation.API.API.Validators.AuthValidators.RefreshTokenValidators
{
    public class RevokeRefreshTokenDtoValidator : AbstractValidator<RevokeRefreshTokenDto>
    {
        public RevokeRefreshTokenDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }
}
