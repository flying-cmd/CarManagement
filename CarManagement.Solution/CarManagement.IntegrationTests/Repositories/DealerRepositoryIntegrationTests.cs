using CarManagement.IntegrationTests.Infrastructure;
using CarManagement.Models.Entities;
using FluentAssertions;

namespace CarManagement.IntegrationTests.Repositories;

public sealed class DealerRepositoryIntegrationTests : IClassFixture<RepositoryTestFixture>
{
    private readonly RepositoryTestFixture _fixture;

    public DealerRepositoryIntegrationTests(RepositoryTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddDealerAsync_ShouldAddDealer()
    {
        // Arrange
        var dealer = Dealer.CreateDealer(
            name: "DealerOne",
            email: $"dealer.{Guid.NewGuid():N}@example.com",
            plainPassword: "Pass123$",
            passwordHasher: _fixture.PasswordHasher);

        // Act
        await _fixture.DealerRepository.AddDealerAsync(dealer, CancellationToken.None);

        // Assert
        var loaded = await _fixture.DealerRepository.GetDealerByEmailAsync(dealer.Email, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(dealer.Id);
        loaded.Name.Should().Be(dealer.Name);
        loaded.Email.Should().Be(dealer.Email);
    }

    [Fact]
    public async Task GetDealerByIdAsync_ShouldGetDealer()
    {
        // Arrange
        var dealer = Dealer.CreateDealer(
            name: "DealerOne",
            email: $"dealer.{Guid.NewGuid():N}@example.com",
            plainPassword: "Pass123$",
            passwordHasher: _fixture.PasswordHasher);

        await _fixture.DealerRepository.AddDealerAsync(dealer, CancellationToken.None);

        // Act
        var result = await _fixture.DealerRepository.GetDealerByIdAsync(dealer.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dealer.Id);
        result.Name.Should().Be(dealer.Name);
        result.Email.Should().Be(dealer.Email);
    }

    [Fact]
    public async Task GetDealerByEmailAsync_ShouldGetDealer()
    {
        // Arrange
        var dealer = Dealer.CreateDealer(
            name: "DealerOne",
            email: $"dealer.{Guid.NewGuid():N}@example.com",
            plainPassword: "Pass123$",
            passwordHasher: _fixture.PasswordHasher);

        await _fixture.DealerRepository.AddDealerAsync(dealer, CancellationToken.None);

        // Act
        var result = await _fixture.DealerRepository.GetDealerByEmailAsync(dealer.Email, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dealer.Id);
        result.Name.Should().Be(dealer.Name);
        result.Email.Should().Be(dealer.Email);
    }
}
