using CarManagement.Models.Entities;
using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using static CarManagement.Repository.Repositories.CarRepository;

namespace CarManagement.Service.Mappers;

public class CarMapper : ResponseMapper<CarResponseDto, CarWithStockRow>
{
    public override CarResponseDto FromEntity(CarWithStockRow car) => new()
    {
        Id = Guid.Parse(car.Id),
        DealerId = Guid.Parse(car.DealerId),
        CarStockId = Guid.Parse(car.CarStockId),
        Make = car.Make,
        Model = car.Model,
        Year = car.Year,
        UnitPrice = car.UnitPrice,
        StockLevel = car.StockLevel,
        CreatedAt = DateTimeOffset.Parse(car.CreatedAt),
        StockUpdatedAt = DateTimeOffset.Parse(car.StockUpdatedAt)
    };
}
