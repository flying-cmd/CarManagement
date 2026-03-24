
using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarManagement.Repository.Interfaces;

public interface ICarRepository
{
    Task AddCarAsync(Car car, CancellationToken ct);
    Task<bool> ExistsAsync(Guid dealerId, string make, string model, int year, string colour, CancellationToken ct);
    Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Car>> ListCarsAsync(Guid dealerId, int pageNumber, int pageSize, CancellationToken ct);
    Task<bool> RemoveCarByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Car>> SearchCarsAsync(Guid dealerId, string? make, string? model, int pageNumber, int pageSize, CancellationToken ct);
    Task<bool> UpdateCarStockLevelByIdAsync(Guid id, int stockLevel, CancellationToken ct);
}
