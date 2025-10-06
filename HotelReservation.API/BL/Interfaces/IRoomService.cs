using Clinic.API.API.Dtos.PagingDtos;
using HotelReservation.API.API.Dtos.RoomDtos;
using HotelReservation.API.Common.Responses;

namespace HotelReservation.API.BL.Interfaces
{
    public interface IRoomService
    {
        Task<ApiResponse<RoomDto>> CreateRoomAsync(CreateRoomDto createRoomDto);
        Task<ApiResponse<RoomDto>> GetRoomByIdAsync(Guid id);
        Task<ApiResponse<PagedResult<RoomDto>>> GetAllRoomsAsync(PagingDto pagingDto);
        Task<ApiResponse<object>> UpdateRoomAsync(Guid id, UpdateRoomDto updateRoomDto);
        Task<ApiResponse<object>> DeleteRoomAsync(Guid id);
    }
}
