using CarManagement.Models.Entities;

namespace CarManagement.Service.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Dealer dealer);
}
