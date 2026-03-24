using CarManagement.Common.Exceptions;
using CarManagement.Models.Entities;
using CarManagement.Service.DTOs.Auth;
using CarManagement.Service.Interfaces;
using CarManagementApi.Repository.Interfaces;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarManagement.Service.Services;

public class AuthService : IAuthService
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IPasswordHasher<Dealer> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IDealerRepository dealerRepository,
        IPasswordHasher<Dealer> passwordHasher,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _dealerRepository = dealerRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
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
        // Check if dealer exists
        var dealer = await _dealerRepository.GetDealerByEmailAsync(req.Email.Trim().ToLowerInvariant(), ct);
        if (dealer is null || _passwordHasher.VerifyHashedPassword(dealer, dealer.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            _logger.LogError("Login Failed: Invalid email or password");
            throw ApiException.Unauthorized("Invalid email or password");
        }

        // Create JWT token
        var (jwtToken, expiresAt) = GenerateJwtToken(dealer);

        _logger.LogInformation($"Dealer with id {dealer.Id} login successful");

        return new AuthResponseDto 
        {
            Name = dealer.Name,
            Email = dealer.Email, 
            AccessToken = jwtToken, 
            ExpiresAtUtc = expiresAt 
        };
    }

    /// <summary>
    /// Register.
    /// </summary>
    /// <param name="req">The register request <see cref="RegisterRequestDto"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="AuthResponseDto"/> if successful.</returns>
    /// <exception cref="ApiException.BadRequest(string)">Thrown if email already exists.</exception>
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req, CancellationToken ct)
    {
        var normalizedName = req.Name.Trim();
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        // Check if email already exists
        var existingDealer = await _dealerRepository.GetDealerByEmailAsync(normalizedEmail, ct);
        if (existingDealer is not null)
        {
            _logger.LogError("Registration Failed: Email already exists");
            throw ApiException.BadRequest("Email already exists");
        }

        // Create new dealer
        var dealer = Dealer.CreateDealer(normalizedName, normalizedEmail, req.Password, _passwordHasher);

        await _dealerRepository.AddDealerAsync(dealer, ct);

        // Create JWT token
        var (jwtToken, expiresAt) = GenerateJwtToken(dealer);

        _logger.LogInformation($"Dealer with id {dealer.Id} registered successfully");

        return new AuthResponseDto
        {
            Name = dealer.Name,
            Email = dealer.Email,
            AccessToken = jwtToken,
            ExpiresAtUtc = expiresAt
        };
    }

    /// <summary>
    /// Generate JWT token.
    /// </summary>
    /// <param name="dealer">The id of the dealer.</param>
    /// <returns>Returns JWT token and expiration date.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Jwt:DurationInMinutes is missing or invalid, or Jwt:SigningKey is missing.</exception>
    private (string token, DateTime expiresAt) GenerateJwtToken(Dealer dealer)
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
