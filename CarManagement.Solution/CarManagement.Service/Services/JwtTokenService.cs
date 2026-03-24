using CarManagement.Models.Entities;
using CarManagement.Service.Interfaces;
using FastEndpoints.Security;
using Microsoft.Extensions.Configuration;

namespace CarManagement.Service.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generate JWT token.
    /// </summary>
    /// <param name="dealer">The id of the dealer.</param>
    /// <returns>Returns JWT token and expiration date.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Jwt:DurationInMinutes is missing or invalid, or Jwt:SigningKey is missing.</exception>
    public (string Token, DateTime ExpiresAt) GenerateToken(Dealer dealer)
    {
        var durationInMinutes = _configuration.GetValue<int?>("Jwt:DurationInMinutes");
        if (!durationInMinutes.HasValue || durationInMinutes.Value <= 0)
        {
            throw new InvalidOperationException("Jwt:DurationInMinutes is missing or invalid.");
        }

        var signingKey = _configuration.GetValue<string>("Jwt:SigningKey");
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey is missing.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(durationInMinutes.Value);

        var jwtToken = JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = signingKey;
                o.ExpireAt = expiresAt;
                o.User.Roles.Add("Dealer");
                o.User.Claims.Add(("Email", dealer.Email));
                o.User["Name"] = dealer.Name;
                o.User["UserId"] = dealer.Id.ToString();
            });

        return (jwtToken, expiresAt);
    }
}
