using CarManagement.Common.Exceptions;
using CarManagement.Models.Entities;
using CarManagement.Service.DTOs.Auth;
using CarManagement.Service.Interfaces;
using CarManagementApi.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CarManagement.Service.Services;

public class AuthService : IAuthService
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IPasswordHasher<Dealer> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IDealerRepository dealerRepository,
        IPasswordHasher<Dealer> passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _dealerRepository = dealerRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
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
        var (jwtToken, expiresAt) = _jwtTokenService.GenerateToken(dealer);

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
        var (jwtToken, expiresAt) = _jwtTokenService.GenerateToken(dealer);

        _logger.LogInformation($"Dealer with id {dealer.Id} registered successfully");

        return new AuthResponseDto
        {
            Name = dealer.Name,
            Email = dealer.Email,
            AccessToken = jwtToken,
            ExpiresAtUtc = expiresAt
        };
    }
}
