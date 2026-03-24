using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;

namespace CarManagement.Service.Interfaces;

public interface ICarService
{
    Task<CarResponseDto> AddCarAsync(AddCarRequestDto req, Guid dealerId, CancellationToken ct);
    Task<PagedResult<CarResponseDto>> ListCarsAsync(ListCarsRequestDto req, Guid dealerId, CancellationToken ct);
    Task RemoveCarByIdAsync(Guid id, Guid dealerId, CancellationToken ct);
    Task UpdateCarStockLevelByIdAsync(Guid id, int stockLevel, Guid dealerId, CancellationToken ct);
}
