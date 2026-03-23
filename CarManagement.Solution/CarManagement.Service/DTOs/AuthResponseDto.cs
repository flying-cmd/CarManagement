namespace CarManagement.Service.DTOs;

public class AuthResponseDto
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
