using FluentValidation;
using HotelReservation.API.API.Dtos.RoomDtos;

namespace HotelReservation.API.API.Validations.RoomValidators
{
    public class UpdateRoomDtoValidator : AbstractValidator<UpdateRoomDto>
    {
        public UpdateRoomDtoValidator()
        {

            RuleFor(x => x.RoomNumber)
               .MaximumLength(10)
               .When(x => !string.IsNullOrWhiteSpace(x.RoomNumber))
               .WithMessage("Room number cannot exceed 10 characters.");
            RuleFor(x => x.RoomType)
                .IsInEnum()
                .When(x => x.RoomType.HasValue)
                .WithMessage("A valid room type is required.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0)
                .When(x => x.Capacity.HasValue)
                .WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x.PricePerNight)
                .GreaterThan(0)
                .When(x => x.PricePerNight.HasValue)
                .WithMessage("Price per night must be greater than 0.");

            // Rule 2: Ensure at least one field is provided for the update.
            RuleFor(x => x)
                .Must(HaveAtLeastOneValue)
                .WithMessage("At least one field must be provided to update the room.");
        }

        private bool HaveAtLeastOneValue(UpdateRoomDto dto)
        {
            // Check if any of the properties have a value.
            // This ensures the request body isn't empty.
            return dto.RoomNumber != null ||
                   dto.Description != null ||
                   dto.RoomType.HasValue ||
                   dto.Capacity.HasValue ||
                   dto.PricePerNight.HasValue ||
                   dto.IsAvailable.HasValue;
        }
    }
}
