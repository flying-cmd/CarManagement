namespace CarManagement.Service.DTOs.Auth;

public sealed class AuthResponseDto
{
    /// <summary>
    /// The name of the user.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The email of the user.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// The access token of the user.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// The expiration date of the access token.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
