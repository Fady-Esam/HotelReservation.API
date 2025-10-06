using Clinic.API.API.Dtos.PagingDtos;
using HotelReservation.API.API.Dtos.RoomDtos;
using HotelReservation.API.BL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Uncomment this to secure the endpoints
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoomById(Guid id)
        {
            var response = await _roomService.GetRoomByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRooms([FromQuery] PagingDto pagingDto)
        {
            var response = await _roomService.GetAllRoomsAsync(pagingDto);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        // [Authorize(Roles = "Admin")] // Example of role-based authorization
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto createRoomDto)
        {
            var response = await _roomService.CreateRoomAsync(createRoomDto);
            return StatusCode(response.StatusCode, response);


        }

        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomDto updateRoomDto)
        {
            var response = await _roomService.UpdateRoomAsync(id, updateRoomDto);
            return StatusCode(response.StatusCode, response);

        }

        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(Guid id)
        {
            var response = await _roomService.DeleteRoomAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
