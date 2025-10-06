
using FluentValidation;
using HotelReservation.API.API.Dtos.AuthDtos;

namespace HotelReservation.API.API.Validators.AuthValidators.RefreshTokenValidators
{
    public class SocialLoginDtoValidator : AbstractValidator<SocialLoginDto>
    {
        public SocialLoginDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }
}
