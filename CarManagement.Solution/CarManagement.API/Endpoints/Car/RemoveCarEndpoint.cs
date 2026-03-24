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
            s.Summary = "Remove car";
            s.Description = "Remove car by given Id";
            s.Params["Id"] = "Id of the car";
            s.Response(StatusCodes.Status204NoContent, "Car removed successfully.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "Car not found.");
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
