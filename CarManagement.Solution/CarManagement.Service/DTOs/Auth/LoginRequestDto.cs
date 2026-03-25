using FastEndpoints;

namespace CarManagement.Service.DTOs.Auth;

public sealed class LoginRequestDto
{
    /// <summary>
    /// The email of the dealer.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// The password of the dealer.
    /// </summary>
    public string Password { get; set; } = null!;
}

