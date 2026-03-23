using CarManagement.Service.DTOs;

namespace CarManagement.Service.Interfaces;

public interface ICarService
{
    Task<CarResponseDto> AddCarAsync(AddCarRequestDto req, Guid dealerId, CancellationToken ct);
}
