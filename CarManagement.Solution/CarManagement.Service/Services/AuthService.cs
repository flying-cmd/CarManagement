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

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct)
    {
        var dealer = await _dealerRepository.GetDealerByEmailAsync(req.Email, ct);

        if (dealer is null || _passwordHasher.VerifyHashedPassword(dealer, dealer.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
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

        return new LoginResponseDto 
        { 
            Email = dealer.Email, 
            AccessToken = jwtToken, 
            ExpiresAtUtc = expiresAt 
        };
    }
}
