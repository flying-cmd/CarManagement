using FastEndpoints;

namespace CarManagement.Service.DTOs;

public sealed class LoginRequestDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

