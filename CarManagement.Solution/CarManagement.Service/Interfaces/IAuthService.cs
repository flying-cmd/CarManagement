using CarManagement.Service.DTOs;

namespace CarManagement.Service.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct);
}
