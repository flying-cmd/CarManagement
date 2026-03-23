using CarManagement.Service.DTOs;

namespace CarManagement.Service.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct);
}
