using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public sealed class ListCarsEndpoint : Endpoint<ListCarsRequestDto, ApiResponse<PagedResult<CarResponseDto>>>
{
    private readonly ICarService _carService;
    private readonly IUserContext _userContext;

    public ListCarsEndpoint(ICarService carService, IUserContext userContext)
    {
        _carService = carService;
        _userContext = userContext;
    }

    /// <summary>
    /// COnfigures the list cars endpoint, authentication, authorization and swagger documentation.
    /// </summary>
    public override void Configure()
    {
        Get("/api/cars");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(RoleNames.DEALER);
        Summary(s =>
        {
            s.Summary = "List cars";
            s.Description = "List cars in pagination.";
            s.RequestParam(request => request.PageNumber, "Page number.");
            s.RequestParam(request => request.PageSize, "Page size. The number of items per page.");
            s.Response<ApiResponse<PagedResult<CarResponseDto>>>(StatusCodes.Status200OK, "List of cars.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "Invalid request.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "Unauthorized.");
        });
    }

    /// <summary>
    /// Handles the list cars request.
    /// </summary>
    /// <param name="req">The list cars request. See <see cref="ListCarsRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(ListCarsRequestDto req, CancellationToken ct)
    {
        var result = await _carService.ListCarsAsync(req, _userContext.DealerId, ct);

        await Send.OkAsync(
            ApiResponse<PagedResult<CarResponseDto>>.SuccessResponse(result),
            ct);
    }
}
