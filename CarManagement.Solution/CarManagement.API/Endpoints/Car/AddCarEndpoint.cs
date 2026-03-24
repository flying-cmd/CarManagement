using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public sealed class AddCarEndpoint : Endpoint<AddCarRequestDto, ApiResponse<CarResponseDto>>
{
    private readonly ICarService _carService;
    private readonly IUserContext _userContext;

    public AddCarEndpoint(
        ICarService carService,
        IUserContext userContext)
    {
        _carService = carService;
        _userContext = userContext;
    }

    public override void Configure()
    {
        Post("/api/cars");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(RoleNames.DEALER);
        Summary(s =>
        {
            s.Summary = "Add car";
            s.Description = "Add car with given Dealer Id, Make, Model, Year, Colour, Price and StockLevel";
            s.RequestParam(request => request.Make, "Make of the car");
            s.RequestParam(request => request.Model, "Model of the car");
            s.RequestParam(request => request.Year, "Year of the car");
            s.RequestParam(request => request.Colour, "Colour of the car");
            s.RequestParam(request => request.Price, "Price of the car");
            s.RequestParam(request => request.StockLevel, "StockLevel of the car");
            s.Response<ApiResponse<CarResponseDto>>(StatusCodes.Status201Created, "Car added successfully.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "Car already exists or invalid request.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "Unauthorized");
        });
    }

    public override async Task HandleAsync(AddCarRequestDto req, CancellationToken ct)
    {
        var result = await _carService.AddCarAsync(req, _userContext.DealerId, ct);
        
        await Send.ResponseAsync(
            ApiResponse<CarResponseDto>.SuccessResponse(result, "Car added successfully."), 
            StatusCodes.Status201Created, 
            ct);
    }
}
