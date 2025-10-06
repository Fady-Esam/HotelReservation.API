using HotelReservation.API.Domain.Entities;

namespace HotelReservation.API.Domain.Interfaces
{
    public interface IRoomRepository : IGenericRepository<Room>
    {
        Task<Room?> GetByIdAsync(Guid id);
    }
}
