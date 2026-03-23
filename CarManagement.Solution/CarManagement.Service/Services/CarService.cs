using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using CarManagement.Service.DTOs;
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
    /// <exception cref="ApiException.NotFound(string)">Thrown if dealer not found.</exception>
    /// <exception cref="ApiException.BadRequest(string)">Thrown if car already exists.</exception>
    public async Task<CarResponseDto> AddCarAsync(AddCarRequestDto req, Guid dealerId, CancellationToken ct)
    {
        // Check if dealer exists
        var dealer = await _dealerRepository.GetDealerByIdAsync(dealerId, ct);
        if (dealer is null)
        {
            _logger.LogError("Add Car Failed: Dealer not found");
            throw ApiException.NotFound("Dealer not found");
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
}
