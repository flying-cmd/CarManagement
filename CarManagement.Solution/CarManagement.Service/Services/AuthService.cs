using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using CarManagement.Service.DTOs;
using CarManagement.Service.Interfaces;
using CarManagementApi.Repository.Interfaces;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CarManagement.Service.Services;

public class AuthService : IAuthService
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IPasswordHasher<Dealer> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IDealerRepository dealerRepository,
        IPasswordHasher<Dealer> passwordHasher,
        IConfiguration configuration)
    {
        _dealerRepository = dealerRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    /// <summary>
    /// Login.
    /// </summary>
    /// <param name="req">The login request <see cref="LoginRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="AuthResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.Unauthorized(string)">Thrown if invalid email or password.</exception>
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct)
    {
        var dealer = await _dealerRepository.GetDealerByEmailAsync(req.Email, ct);

        if (dealer is null || _passwordHasher.VerifyHashedPassword(dealer, dealer.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            throw ApiException.Unauthorized("Invalid email or password");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationInMinutes"));

        var jwtToken = JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = _configuration.GetValue<string>("Jwt:SigningKey")!;
                o.ExpireAt = expiresAt;
                o.User.Roles.Add("Dealer");
                o.User.Claims.Add(("Email", dealer.Email));
                o.User["Name"] = dealer.Name;
                o.User["UserId"] = dealer.Id.ToString();
            });

        return new AuthResponseDto 
        {
            Name = dealer.Name,
            Email = dealer.Email, 
            AccessToken = jwtToken, 
            ExpiresAtUtc = expiresAt 
        };
    }
}
