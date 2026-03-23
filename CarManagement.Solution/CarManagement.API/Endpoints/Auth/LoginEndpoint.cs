using CarManagement.Service.DTOs;
using CarManagement.Service.Interfaces;
using FastEndpoints;

namespace CarManagement.API.Endpoints.Auth;

public class LoginEndpoint : Endpoint<LoginRequestDto, LoginResponseDto>
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
    }

    public override async Task HandleAsync(LoginRequestDto req, CancellationToken ct)
    {
        var res = await _authService.LoginAsync(req, ct);

        await Send.OkAsync(res);
    }
}
