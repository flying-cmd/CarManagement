using Microsoft.AspNetCore.Identity;

namespace CarManagement.Models.Entities;

public class Dealer
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; init; }

    public static Dealer CreateDealer(
        string name,
        string email,
        string plainPassword,
        IPasswordHasher<Dealer> passwordHasher)
    {
        var dealer = new Dealer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dealer.PasswordHash = passwordHasher.HashPassword(dealer, plainPassword);

        return dealer;
    }
}
