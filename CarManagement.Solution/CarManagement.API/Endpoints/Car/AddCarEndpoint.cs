using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public class AddCarEndpoint : Endpoint<AddCarRequestDto, ApiResponse<CarResponseDto>>
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
