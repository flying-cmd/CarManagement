
using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using System.Data;
using static CarManagement.Repository.Repositories.CarRepository;

namespace CarManagement.Repository.Interfaces;

public interface ICarRepository
{
    Task<bool> AddCarAsync(Car car, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<Car?> GetByMakeModelYearAsync(string make, string model, int year, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<PagedResult<CarWithStockRow>> ListCarsAsync(Guid dealerId, int pageNumber, int pageSize, CancellationToken ct);
    Task<bool> RemoveCarByIdAsync(Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<PagedResult<CarWithStockRow>> SearchCarsAsync(Guid dealerId, string? make, string? model, int pageNumber, int pageSize, CancellationToken ct);
    Task<bool> AddCarStockAsync(CarStock carStock, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<bool> ExistsAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<bool> ExistsAsync(Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<bool> RemoveCarStockAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<bool> UpdateCarStockLevelAsync(Guid carId, Guid dealerId, int stockLevel, CancellationToken ct);
}
