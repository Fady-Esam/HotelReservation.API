using FluentValidation;
using HotelReservation.API.API.Dtos.RoomDtos;

namespace HotelReservation.API.API.Validations.RoomValidators
{
    public class CreateRoomDtoValidator : AbstractValidator<CreateRoomDto>
    {
        public CreateRoomDtoValidator()
        {
            RuleFor(x => x.RoomNumber)
             .NotEmpty().WithMessage("Room number is required.")
             .MaximumLength(10).WithMessage("Room number cannot exceed 10 characters.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x.PricePerNight)
                .GreaterThan(0).WithMessage("Price per night must be greater than 0.");

            // 👈 New rule to validate the enum
            RuleFor(x => x.RoomType)
                .IsInEnum().WithMessage("A valid room type is required.");
        }
    }
}
