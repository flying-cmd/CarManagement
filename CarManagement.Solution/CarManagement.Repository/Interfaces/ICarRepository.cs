
using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using System.Data;
using static CarManagement.Repository.Repositories.CarRepository;

namespace CarManagement.Repository.Interfaces;

public interface ICarRepository
{
    /// <summary>
    /// Add new car to car table.
    /// </summary>
    /// <param name="car">The car entity to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if added successfully, otherwise false.</returns>
    Task<bool> AddCarAsync(Car car, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Get car by make, model and year.
    /// </summary>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns <see cref="Car"/> if found, otherwise null.</returns>
    Task<Car?> GetByMakeModelYearAsync(string make, string model, int year, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Get car by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns <see cref="Car"/> if found, otherwise null.</returns>
    Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// List cars by given dealer id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="CarWithStockRow"/>.</returns>
    Task<PagedResult<CarWithStockRow>> ListCarsAsync(Guid dealerId, int pageNumber, int pageSize, CancellationToken ct);

    /// <summary>
    /// Remove car from the Car table by id.
    /// </summary>
    /// <param name="carId">The id of the car to remove.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if the car was removed, otherwise false.</returns>
    Task<bool> RemoveCarByIdAsync(Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Search cars owned by the given dealer with optional make and model filters.
    /// </summary>
    /// <param name="dealerId">The id of the dealer whose cars to search.</param>
    /// <param name="make">Optional. The make of the car. When provided, cars whose make contains this string will be returned.</param>
    /// <param name="model">Optional. The model of the car. When provided, cars whose model contains this string will be returned.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size. The number of items to return per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="CarWithStockRow"/>.</returns>
    Task<PagedResult<CarWithStockRow>> SearchCarsAsync(Guid dealerId, string? make, string? model, int pageNumber, int pageSize, CancellationToken ct);

    /// <summary>
    /// Create new car stock.
    /// </summary>
    /// <param name="carStock">The car stock entity <see cref="CarStock"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if the car stock was created, otherwise false.</returns>
    Task<bool> AddCarStockAsync(CarStock carStock, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Check if the car stock exists by given the dealer id and car id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if the car stock exists, otherwise false.</returns>
    Task<bool> ExistsAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Check if there is no stock left for this car for all dealers.
    /// </summary>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if there is no stock left for all dealers, otherwise false.</returns>
    Task<bool> ExistsAsync(Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Check if car exists given the dealer id, make, model and year.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car exists, otherwise false.</returns>
    Task<bool> ExistsAsync(Guid dealerId, string make, string model, int year, CancellationToken ct);

    /// <summary>
    /// Remove car stock.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">Optional. The DB connection.</param>
    /// <param name="transaction">Optional. The DB transaction.</param>
    /// <returns>Returns true if the car stock was removed, otherwise false.</returns>
    Task<bool> RemoveCarStockAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Update car stock level by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="stockLevel">The new stock level.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the stock level was updated, otherwise false.</returns>
    Task<bool> UpdateCarStockLevelAsync(Guid carId, Guid dealerId, int stockLevel, CancellationToken ct);
}
