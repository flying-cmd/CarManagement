using CarManagement.Models.Entities;
using CarManagement.Service.DTOs.Car;
using FastEndpoints;

namespace CarManagement.Service.Mappers;

public class CarMapper : ResponseMapper<CarResponseDto, Car>
{
    public override CarResponseDto FromEntity(Car car) => new()
    {
        Id = car.Id,
        DealerId = car.DealerId,
        Make = car.Make,
        Model = car.Model,
        Year = car.Year,
        Colour = car.Colour,
        Price = car.Price,
        StockLevel = car.StockLevel,
        CreatedAt = car.CreatedAt,
        UpdatedAt = car.UpdatedAt
    };
}
