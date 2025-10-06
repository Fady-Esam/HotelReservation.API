using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Interfaces;

namespace HotelReservation.API.DL.Repositories
{
    public class RoomRepository : GenericRepository<Room>, IRoomRepository
    {
        public RoomRepository(ApplicationDbContext context, ILogger<RoomRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<Room?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting Room entity with ID {RoomId}", id);

            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                _logger.LogWarning("Room with ID {RoomId} was not found.", id);
            }

            return room;
        }
    }
}
