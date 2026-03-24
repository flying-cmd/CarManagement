using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Auth;
using CarManagement.Service.Interfaces;
using FastEndpoints;

namespace CarManagement.API.Endpoints.Auth;

public sealed class LoginEndpoint : Endpoint<LoginRequestDto, ApiResponse<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public LoginEndpoint(
        IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("api/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Dealer login";
            s.Description = "Authenticates a dealer using email and password, then returns a JWT access token.";
            s.RequestParam(request => request.Email, "The dealer's email address.");
            s.RequestParam(request => request.Password, "The dealer's password.");
            s.Response<ApiResponse<AuthResponseDto>>(StatusCodes.Status200OK, "Login successfully.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status400BadRequest, "The login request failed validation.");
            s.Response<ApiResponse<object?>>(StatusCodes.Status401Unauthorized, "The email or password is incorrect.");
        });
    }

    public override async Task HandleAsync(LoginRequestDto req, CancellationToken ct)
    {
        var res = await _authService.LoginAsync(req, ct);

        await Send.OkAsync(ApiResponse<AuthResponseDto>.SuccessResponse(res, "Login successfully"), ct);
    }
}
