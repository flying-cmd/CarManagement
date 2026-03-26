using CarManagement.Common.Constants;
using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CarManagement.API.Endpoints.Car;

public sealed class SearchCarEndpoint : Endpoint<SearchCarRequestDto, ApiResponse<PagedResult<CarResponseDto>>>
{
    private readonly ICarService _carService;
    private readonly IUserContext _userContext;

    public SearchCarEndpoint(ICarService carService, IUserContext userContext)
    {
        _carService = carService;
        _userContext = userContext;
    }

    /// <summary>
    /// Configure the search car endpoint, authentication, authorization and swagger documentation.
    /// </summary>
    public override void Configure()
    {
        Get("/api/cars/search");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Roles(RoleNames.DEALER);
        Summary(s =>
        {
            s.Summary = "Search cars";
            s.Description = "Search cars in pagination.";
            s.RequestParam(request => request.Make, "Optional. Make of the car.");
            s.RequestParam(request => request.Model, "Optional. Model of the car.");
            s.RequestParam(request => request.PageNumber, "Page number.");
            s.RequestParam(request => request.PageSize, "Page size. The number of items per page.");
            s.Response<ApiResponse<PagedResult<CarResponseDto>>>(StatusCodes.Status200OK, "List of cars.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "Invalid request.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "Unauthorized.");
        });
    }

    /// <summary>
    /// Handle the search car request.
    /// </summary>
    /// <param name="req">The search car request.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(SearchCarRequestDto req, CancellationToken ct)
    {
        var result = await _carService.SearchCarsAsync(req, _userContext.DealerId, ct);

        await Send.OkAsync(ApiResponse<PagedResult<CarResponseDto>>.SuccessResponse(result), ct);
    }
}
