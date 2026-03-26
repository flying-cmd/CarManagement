using CarManagement.Models.Entities;

namespace CarManagement.Service.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Generate JWT token.
    /// </summary>
    /// <param name="dealer">The id of the dealer.</param>
    /// <returns>Returns JWT token and expiration date.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Jwt:DurationInMinutes is missing or invalid, or Jwt:SigningKey is missing.</exception>
    (string Token, DateTime ExpiresAt) GenerateToken(Dealer dealer);
}
