namespace CarManagement.Service.DTOs;

public class LoginResponseDto
{
    public string Email { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
