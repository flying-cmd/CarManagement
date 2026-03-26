namespace CarManagement.Service.DTOs.Auth;

public sealed class RegisterRequestDto
{
    /// <summary>
    /// The dealer's name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The dealer's email.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// The dealer's phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// The dealer's password. Must be at least 6 characters long and cannot exceed 20 characters. 
    /// Must contain at least one uppercase letter, one lowercase letter, one number, and one special character.
    /// </summary>
    public string Password { get; set; } = null!;
}
