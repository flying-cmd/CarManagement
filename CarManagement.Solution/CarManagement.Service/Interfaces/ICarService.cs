using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;

namespace CarManagement.Service.Interfaces;

public interface ICarService
{
    /// <summary>
    /// Add car and create car stock.
    /// If the car already exists, reuse it and create a new car stock for the current dealer.
    /// Otherwise, create a new car and a new car stock for the current dealer.
    /// </summary>
    /// <param name="req">The add car request <see cref="AddCarRequestDto"/>.</param>
    /// <param name="dealerId">The dealer id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="CarResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if dealer does not exist.</exception>
    /// <exception cref="ApiException.BadRequest(string)">Thrown if car already exists.</exception>
    /// <exception cref="ApiException.InternalServerError(string)">Thrown if internal server error.</exception>
    Task<CarResponseDto> AddCarAsync(AddCarRequestDto req, Guid dealerId, CancellationToken ct);

    /// <summary>
    /// List cars in pagination.
    /// </summary>
    /// <param name="req">The request <see cref="ListCarsRequestDto"/>.</param>
    /// <param name="dealerId">The dealer id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="CarResponseDto"/>.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if dealer does not exist.</exception>

    Task<PagedResult<CarResponseDto>> ListCarsAsync(ListCarsRequestDto req, Guid dealerId, CancellationToken ct);

    /// <summary>
    /// Remove car's stock level. If there is no stock level left for all dealers, remove the car.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    /// <exception cref="ApiException.NotFound(string)">Thrown if the car is not found.</exception>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if the dealer does not exist.</exception>
    /// <exception cref="ApiException.Forbidden(string)">Thrown if the car does not belong to the dealer.</exception>
    Task RemoveCarByIdAsync(Guid id, Guid dealerId, CancellationToken ct);

    /// <summary>
    /// Search cars owned by the given dealer with optional make and model filters.
    /// </summary>
    /// <param name="req">The search request <see cref="SearchCarRequestDto"/>.</param>
    /// <param name="dealerId">The id of the dealer whose cars to search.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="CarResponseDto"/></returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if the dealer does not exist.</exception>
    Task<PagedResult<CarResponseDto>> SearchCarsAsync(SearchCarRequestDto req, Guid dealerId, CancellationToken ct);

    /// <summary>
    /// Update car stock level.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="stockLevel">The new stock level.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    /// <exception cref="ApiException.NotFound(string)">Thrown if the car is not found.</exception>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if the dealer does not exist.</exception>
    /// <exception cref="ApiException.Forbidden(string)">Thrown if the car does not belong to the dealer.</exception>
    Task UpdateCarStockLevelByIdAsync(Guid id, int stockLevel, Guid dealerId, CancellationToken ct);
}
