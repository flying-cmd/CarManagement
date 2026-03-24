using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using CarManagement.Service.Mappers;
using CarManagementApi.Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace CarManagement.Service.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _carRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly ILogger<CarService> _logger;
    private readonly CarMapper _carMapper;

    public CarService(
        ICarRepository carRepository,
        IDealerRepository dealerRepository,
        ILogger<CarService> logger,
        CarMapper carMapper)
    {
        _carRepository = carRepository;
        _dealerRepository = dealerRepository;
        _logger = logger;
        _carMapper = carMapper;
    }

    /// <summary>
    /// Add car.
    /// </summary>
    /// <param name="req">The add car request <see cref="AddCarRequestDto"/>.</param>
    /// <param name="dealerId">The dealer id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="CarResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if dealer does not exist.</exception>
    /// <exception cref="ApiException.BadRequest(string)">Thrown if car already exists.</exception>
    public async Task<CarResponseDto> AddCarAsync(AddCarRequestDto req, Guid dealerId, CancellationToken ct)
    {
        // Check if dealer exists
        var dealer = await _dealerRepository.GetDealerByIdAsync(dealerId, ct);
        if (dealer is null)
        {
            _logger.LogError("Add Car Failed: Unauthorized. Dealer not found");
            throw ApiException.Unauthorized("Unauthorized");
        }

        // Check if car already exists
        if (await _carRepository.ExistsAsync(dealerId, req.Make, req.Model, req.Year, req.Colour, ct))
        {
            _logger.LogError("Add Car Failed: Car already exists");
            throw ApiException.BadRequest("Car already exists");
        }

        // Add car
        var car = new Car(dealerId, req.Make, req.Model, req.Year, req.Colour, req.Price, req.StockLevel);

        await _carRepository.AddCarAsync(car, ct);

        return _carMapper.FromEntity(car);
    }

    /// <summary>
    /// List cars in pagination.
    /// </summary>
    /// <param name="req">The request <see cref="ListCarsRequestDto"/>.</param>
    /// <param name="dealerId">The dealer id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="CarResponseDto"/>.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if dealer does not exist.</exception>
    public async Task<PagedResult<CarResponseDto>> ListCarsAsync(ListCarsRequestDto req, Guid dealerId, CancellationToken ct)
    {
        // Check if dealer exists
        var dealer = await _dealerRepository.GetDealerByIdAsync(dealerId, ct);
        if (dealer is null)
        {
            _logger.LogError("List Cars Failed: Unauthorized. Dealer is not found.");
            throw ApiException.Unauthorized("Unauthorized");
        }

        // List cars
        var cars = await _carRepository.ListCarsAsync(dealerId, req.PageNumber, req.PageSize, ct);

        return new PagedResult<CarResponseDto>
        {
            Items = cars.Items.Select(car => _carMapper.FromEntity(car)).ToList(),
            PageNumber = cars.PageNumber,
            PageSize = cars.PageSize,
            TotalCount = cars.TotalCount
        };
    }

    /// <summary>
    /// Remove car.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    /// <exception cref="ApiException.NotFound(string)">Thrown if the car is not found.</exception>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if the dealer does not exist.</exception>
    /// <exception cref="ApiException.Forbidden(string)">Thrown if the car does not belong to the dealer.</exception>
    public async Task RemoveCarByIdAsync(Guid id, Guid dealerId, CancellationToken ct)
    {
        // Check if the dealer exists
        var dealer = await _dealerRepository.GetDealerByIdAsync(dealerId, ct);
        if (dealer is null)
        {
            _logger.LogError("Remove Car Failed: Unauthorized. Dealer not found");
            throw ApiException.Unauthorized("Unauthorized");
        }

        // Check if the car exists
        var car = await _carRepository.GetCarByIdAsync(id, ct);
        if (car is null)
        {
            _logger.LogError("Remove Car Failed: Car not found");
            throw ApiException.NotFound("Car not found");
        }

        // Check if the car belongs to the dealer
        if (car.DealerId != dealerId)
        {
            _logger.LogError("Remove Car Failed: Car does not belong to the dealer");
            throw ApiException.Forbidden("You are not authorized to remove this car");
        }

        // Remove car
        var result = await _carRepository.RemoveCarByIdAsync(id, ct);

        if (!result)
        {
            _logger.LogError("Remove Car Failed: Car not found");
            throw ApiException.NotFound("Car not found");
        }

        _logger.LogInformation("Car removed successfully");
    }

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
    public async Task UpdateCarStockLevelByIdAsync(Guid id, int stockLevel, Guid dealerId, CancellationToken ct)
    {
        // Check if the dealer exists
        var dealer = await _dealerRepository.GetDealerByIdAsync(dealerId, ct);
        if (dealer is null)
        {
            _logger.LogError("Update Car Stock Level Failed: Unauthorized. Dealer not found");
            throw ApiException.Unauthorized("Unauthorized");
        }

        // Check if the car exists
        var car = await _carRepository.GetCarByIdAsync(id, ct);
        if (car is null)
        {
            _logger.LogError("Update Car Stock Level Failed: Car not found");
            throw ApiException.NotFound("Car not found");
        }

        // Check if the car belongs to the dealer
        if (car.DealerId != dealerId)
        {
            _logger.LogError("Update Car Stock Level Failed: Car does not belong to the dealer");
            throw ApiException.Forbidden("You are not authorized to update this car");
        }

        // Update car stock level
        var result = await _carRepository.UpdateCarStockLevelByIdAsync(id, stockLevel, ct);

        if (!result)
        {
            _logger.LogError("Update Car Stock Level Failed: Car not found");
            throw ApiException.NotFound("Car not found");
        }

        _logger.LogInformation($"Car stock level updated to {stockLevel} successfully");
    }
}
