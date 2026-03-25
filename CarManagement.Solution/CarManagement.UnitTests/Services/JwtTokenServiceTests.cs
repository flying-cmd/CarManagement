using CarManagement.Models.Entities;
using CarManagement.Service.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CarManagement.UnitTests.Services;

public class JwtTokenServiceTests
{
    /// <summary>
    /// Builds the configuration.
    /// </summary>
    /// <param name="durationInMinutes">The duration in minutes.</param>
    /// <param name="signingKey">The signing key.</param>
    /// <returns>Returns the built configuration.</returns>
    private static IConfiguration BuildConfiguration(
        int? durationInMinutes = 60,
        string? signingKey = "super-secret-signing-key-for-tests-1234567890")
    {
        var data = new Dictionary<string, string?>();

        if (durationInMinutes is not null)
        {
            data["Jwt:DurationInMinutes"] = durationInMinutes.ToString();
        }

        if (signingKey is not null)
        {
            data["Jwt:SigningKey"] = signingKey;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static Dealer CreateDealer(
        string name = "Dealer One",
        string email = "dealer@example.com",
        string password = "P@ssword123!")
    {
        var hasher = new PasswordHasher<Dealer>();
        return Dealer.CreateDealer(name, email, password, hasher);
    }

    [Fact]
    public void GenerateToken_WhenDurationInMinutesIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration(durationInMinutes: null);
        var dealer = CreateDealer();
        
        var sut = new JwtTokenService(configuration);

        // Act
        var act = () => sut.GenerateToken(dealer);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("Jwt:DurationInMinutes is missing or invalid.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void GenerateToken_WhenDurationInMinutesIsInvalid_ShouldThrowInvalidOperationException(int durationInMinutes)
    {
        // Arrange
        var configuration = BuildConfiguration(durationInMinutes: durationInMinutes);
        var dealer = CreateDealer();
        
        var sut = new JwtTokenService(configuration);

        // Act
        var act = () => sut.GenerateToken(dealer);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("Jwt:DurationInMinutes is missing or invalid.");
    }

    [Fact]
    public void GenerateToken_WhenSigningKeyIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration(signingKey: null);
        var dealer = CreateDealer();
        
        var sut = new JwtTokenService(configuration);

        // Act
        var act = () => sut.GenerateToken(dealer);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("Jwt:SigningKey is missing.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WhenSigningKeyIsEmptyOrWhitespace_ShouldThrowInvalidOperationException(string signingKey)
    {
        // Arrange
        var configuration = BuildConfiguration(signingKey: signingKey);
        var dealer = CreateDealer();
        
        var sut = new JwtTokenService(configuration);

        // Act
        var act = () => sut.GenerateToken(dealer);

        // Assert
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("Jwt:SigningKey is missing.");
    }
}
