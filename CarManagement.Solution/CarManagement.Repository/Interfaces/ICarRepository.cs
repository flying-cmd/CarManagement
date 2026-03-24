
using CarManagement.Models.Entities;

namespace CarManagement.Repository.Interfaces;

public interface ICarRepository
{
    Task AddCarAsync(Car car, CancellationToken ct);
    Task<bool> ExistsAsync(Guid dealerId, string make, string model, int year, string colour, CancellationToken ct);
    Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct);
    Task<bool> RemoveCarByIdAsync(Guid id, CancellationToken ct);
}
