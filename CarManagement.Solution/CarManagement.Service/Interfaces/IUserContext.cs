namespace CarManagement.Service.Interfaces;

/// <summary>
/// Gets the dealer id of the currently authenticated user from the HTTP context claims.
/// </summary>
/// <exception cref="ApiException.Unauthorized(string)">Thrown when no HTTP context is available, or when the user id claim is missing or invalid.</exception>
public interface IUserContext
{
    /// <summary>
    /// Gets the dealer id of the current user.
    /// </summary>
    Guid DealerId { get; }
}
