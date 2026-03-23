using Microsoft.AspNetCore.Identity;

namespace CarManagement.Models.Entities;

public class Dealer
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="Dealer"/>.
    /// </summary>
    /// <param name="name">The name of the dealer.</param>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="plainPassword">The plain text password of the dealer.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    /// <returns>Returns the new <see cref="Dealer"/>.</returns>
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

    /// <summary>
    /// Rehydrates a <see cref="Dealer"/>. Rebuilds the existing <see cref="Dealer"/> from the database.
    /// </summary>
    /// <param name="id">The id of the dealer.</param>
    /// <param name="name">The name of the dealer.</param>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="passwordHash">The password hash of the dealer.</param>
    /// <param name="createdAt">The creation date of the dealer.</param>
    /// <returns>Returns the rehydrated <see cref="Dealer"/>.</returns>
    public static Dealer Rehydrate(
        Guid id,
        string name,
        string email,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        return new Dealer
        {
            Id = id,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt
        };
    }
}
