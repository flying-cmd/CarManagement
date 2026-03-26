using CarManagement.Service.DTOs.Auth;

namespace CarManagement.Service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Login.
    /// </summary>
    /// <param name="req">The login request <see cref="LoginRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="AuthResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if invalid email or password.</exception>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct);

    /// <summary>
    /// Register.
    /// </summary>
    /// <param name="req">The register request <see cref="RegisterRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="AuthResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.BadRequest(string)">Thrown if email already exists.</exception>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req, CancellationToken ct);
}
