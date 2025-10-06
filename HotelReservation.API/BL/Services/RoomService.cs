using AutoMapper;
using AutoMapper.QueryableExtensions;
using Clinic.API.API.Dtos.PagingDtos;
using HotelReservation.API.API.Dtos.RoomDtos;
using HotelReservation.API.BL.Interfaces;
using HotelReservation.API.Common.Extensions;
using HotelReservation.API.Common.Responses;
using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HotelReservation.API.BL.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<RoomService> _logger;
        private readonly IMemoryCache _cache;
        public RoomService(IRoomRepository roomRepo, IMapper mapper, ILogger<RoomService> logger)
        {
            _roomRepo = roomRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<RoomDto>> CreateRoomAsync(CreateRoomDto createRoomDto)
        {
            _logger.LogInformation("Creating a new room with number {RoomNumber}", createRoomDto.RoomNumber);
            var room = _mapper.Map<Room>(createRoomDto);

            await _roomRepo.AddAsync(room);

            var roomDto = _mapper.Map<RoomDto>(room);
            return ApiResponse<RoomDto>.Success(roomDto, "Room created successfully.", StatusCodes.Status201Created);
        }

        public async Task<ApiResponse<RoomDto>> GetRoomByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting room by ID: {RoomId}", id);
            var room = await _roomRepo.GetByIdAsync(id);

            if (room == null)
            {
                _logger.LogWarning("Room with ID: {RoomId} not found.", id);
                return ApiResponse<RoomDto>.Failure("Room not found.", new() { $"No room found with ID {id}" }, StatusCodes.Status404NotFound);
            }

            var roomDto = _mapper.Map<RoomDto>(room);
            return ApiResponse<RoomDto>.Success(roomDto, "Room retrieved successfully.");
        }

        public async Task<ApiResponse<PagedResult<RoomDto>>> GetAllRoomsAsync(PagingDto pagingDto)
        {
            _logger.LogInformation("Getting all rooms with pagination. Page: {Page}, PageSize: {PageSize}", pagingDto.Page, pagingDto.PageSize);

            var query = _roomRepo.GetAll();

            if (!string.IsNullOrWhiteSpace(pagingDto.Search))
            {
                var search = pagingDto.Search.ToLower();
                query = query.Where(r => r.RoomNumber.ToLower().Contains(search) || (r.Description != null && r.Description.ToLower().Contains(search)));
            }

            var pagedRooms = await query
                .ProjectTo<RoomDto>(_mapper.ConfigurationProvider) // Efficiently projects to DTO
                .ToPagedResultAsync(pagingDto.Page, pagingDto.PageSize);


            return ApiResponse<PagedResult<RoomDto>>.Success(pagedRooms, "Rooms retrieved successfully.");
        }

        public async Task<ApiResponse<object>> UpdateRoomAsync(Guid id, UpdateRoomDto updateRoomDto)
        {
            _logger.LogInformation("Updating room with ID: {RoomId}", id);
            var room = await _roomRepo.GetByIdAsync(id);

            if (room == null)
            {
                _logger.LogWarning("Room with ID: {RoomId} not found for update.", id);
                return ApiResponse<object>.Failure("Room not found.", new() { $"No room found with ID {id}" }, StatusCodes.Status404NotFound);
            }

            _mapper.Map(updateRoomDto, room);
            room.UpdatedAt = DateTime.UtcNow;

            _roomRepo.Update(room);

            return ApiResponse<object>.SuccessNoData("Room updated successfully.", StatusCodes.Status200OK);
        }

        public async Task<ApiResponse<object>> DeleteRoomAsync(Guid id)
        {
            _logger.LogInformation("Deleting room with ID: {RoomId}", id);
            var room = await _roomRepo.GetByIdAsync(id);

            if (room == null)
            {
                _logger.LogWarning("Room with ID: {RoomId} not found for deletion.", id);
                return ApiResponse<object>.Failure("Room not found.", new() { $"No room found with ID {id}" }, StatusCodes.Status404NotFound);
            }

            await _roomRepo.Remove(room);

            return ApiResponse<object>.SuccessNoData("Room deleted successfully.", StatusCodes.Status200OK);
        }
    }
}
