using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public sealed class UpdateCarStockLevelEndpoint : Endpoint<UpdateCarStockLevelRequestDto>
{
    private readonly ICarService _carService;
    private readonly IUserContext _userContext;

    public UpdateCarStockLevelEndpoint(ICarService carService, IUserContext userContext)
    {
        _carService = carService;
        _userContext = userContext;
    }

    public override void Configure()
    {
        Patch("/api/cars/{Id:guid}/stock-level");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(RoleNames.DEALER);
        Summary(s =>
        {
            s.Summary = "Update car stock level";
            s.Description = "Update car stock level by given Id and new stock level";
            s.Params["Id"] = "Id of the car";
            s.RequestParam(request => request.StockLevel, "StockLevel of the car");
            s.Response(StatusCodes.Status204NoContent, "Car stock level updated successfully");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "Invalid request");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "Unauthorized");
            s.Response<ApiResponse<object?>>(StatusCodes.Status403Forbidden, "Forbidden");
            s.Response<ApiResponse<object?>>(StatusCodes.Status404NotFound, "Car not found");
        });
    }

    public override async Task HandleAsync(UpdateCarStockLevelRequestDto req, CancellationToken ct)
    {
        await _carService.UpdateCarStockLevelByIdAsync(req.Id, req.StockLevel, _userContext.DealerId, ct);

        await Send.NoContentAsync(ct);
    }
}
