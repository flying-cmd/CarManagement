using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Auth;
using CarManagement.Service.Interfaces;
using FastEndpoints;

namespace CarManagement.API.Endpoints.Auth;

public sealed class RegisterEndpoint : Endpoint<RegisterRequestDto, ApiResponse<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public RegisterEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Configures the registration endpoint route.
    /// </summary>
    public override void Configure()
    {
        Post("api/auth/register");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Dealer registration";
            s.Description = "Registers a new dealer using email and password, then returns a JWT access token.";
            s.RequestParam(request => request.Name, "The dealer's name.");
            s.RequestParam(request => request.Email, "The dealer's email address.");
            s.RequestParam(request => request.PhoneNumber, "The dealer's phone number.");
            s.RequestParam(request => request.Password, "The dealer's password. Must be at least 6 characters long and cannot exceed 20 characters. Must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            s.Response<ApiResponse<AuthResponseDto>>(StatusCodes.Status201Created, "Registration successfully.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "The registration request failed validation.");
        });
    }

    /// <summary>
    /// Handles the registration request.
    /// </summary>
    /// <param name="req">The registration request. See <see cref="RegisterRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(RegisterRequestDto req, CancellationToken ct)
    {
        var res = await _authService.RegisterAsync(req, ct);

        await Send.ResponseAsync(ApiResponse<AuthResponseDto>.SuccessResponse(res, "Registration successfully"), StatusCodes.Status201Created, ct);
    }
}
