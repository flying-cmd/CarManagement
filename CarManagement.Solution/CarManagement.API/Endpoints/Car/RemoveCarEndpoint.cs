using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public sealed class RemoveCarEndpoint : Endpoint<RemoveCarRequestDto>
{
    private readonly ICarService _carService;
    private readonly IUserContext _userContext;

    public RemoveCarEndpoint(ICarService carService, IUserContext userContext)
    {
        _carService = carService;
        _userContext = userContext;
    }

    public override void Configure()
    {
        Delete("/api/cars/{Id:guid}");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(RoleNames.DEALER);
        Summary(s =>
        {
            s.Summary = "Remove car's stock record for the current dealer. And if there is no stock left for all dealers, remove the car.";
            s.Description = "Remove the current dealer's stock record for the given car Id. And if there is no stock left for all dealers, remove the car.";
            s.Params["Id"] = "Id of the car";
            s.Response(StatusCodes.Status204NoContent, "Car's stock record removed successfully.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status404NotFound, "Car not found.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "Unauthorized.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status403Forbidden, "Forbidden.");
        });
    }

    public override async Task HandleAsync(RemoveCarRequestDto req, CancellationToken ct)
    {
        await _carService.RemoveCarByIdAsync(req.Id, _userContext.DealerId, ct);

        await Send.NoContentAsync(ct);
    }
}
