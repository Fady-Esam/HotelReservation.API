

namespace HotelReservation.API.BL.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationCodeAsync(string email, string code, string subject = "HotelReservation API - Confirmation Code");
        Task SendPasswordResetCodeAsync(string email, string code);
    }
}
