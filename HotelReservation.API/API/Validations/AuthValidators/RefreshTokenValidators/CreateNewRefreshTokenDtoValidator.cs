using FluentValidation;
using HotelReservation.API.API.Dtos.AuthDtos.RefreshTokenDtos;

namespace HotelReservation.API.API.Validators.AuthValidators.RefreshTokenValidators
{
    public class CreateNewRefreshTokenDtoValidator : AbstractValidator<CreateNewRefreshTokenDto>
    {
        public CreateNewRefreshTokenDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }
}
