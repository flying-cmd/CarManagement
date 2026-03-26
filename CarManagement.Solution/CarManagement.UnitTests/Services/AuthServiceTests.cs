using CarManagement.Common.Exceptions;
using CarManagement.Models.Entities;
using CarManagement.Service.DTOs.Auth;
using CarManagement.Service.Interfaces;
using CarManagement.Service.Services;
using CarManagementApi.Repository.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarManagement.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IDealerRepository> _dealerRepositoryMock;
    private readonly Mock<IPasswordHasher<Dealer>> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IAuthService _sut;

    public AuthServiceTests()
    {
        _dealerRepositoryMock = new Mock<IDealerRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher<Dealer>>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(
            _dealerRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailAndPasswordAreValid_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "  dealer@example.com  ",
            Password = "P@ssword123!"
        };
        var dealer = Dealer.CreateDealer("Dealer One", "dealer@example.com", "0400000000", "P@ssword123!", _passwordHasherMock.Object);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(60);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealer);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(dealer, dealer.PasswordHash, "P@ssword123!"))
            .Returns(PasswordVerificationResult.Success);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(dealer))
            .Returns(("fake-jwt-token", expiresAtUtc));

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Dealer One");
        result.Email.Should().Be("dealer@example.com");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().Be(expiresAtUtc);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
        _passwordHasherMock.Verify(
            x => x.VerifyHashedPassword(dealer, dealer.PasswordHash, "P@ssword123!"),
            Times.Once);
        _jwtTokenServiceMock.Verify(
            x => x.GenerateToken(dealer),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidEmail_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = " dealer@example.com ",
            Password = "P@ssword123!"
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Invalid email or password");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
        _passwordHasherMock.Verify(
            x => x.VerifyHashedPassword(It.IsAny<Dealer>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _jwtTokenServiceMock.Verify(
            x => x.GenerateToken(It.IsAny<Dealer>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = " dealer@example.com ",
            Password = "P@ssword123!"
        };
        var dealer = Dealer.CreateDealer("Dealer One", "dealer@example.com", "0400000000", "Abc123!", _passwordHasherMock.Object);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealer);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(dealer, dealer.PasswordHash, "P@ssword123!"))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Invalid email or password");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
        _passwordHasherMock.Verify(
            x => x.VerifyHashedPassword(dealer, dealer.PasswordHash, "P@ssword123!"),
            Times.Once);
        _jwtTokenServiceMock.Verify(
            x => x.GenerateToken(It.IsAny<Dealer>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowConflict()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "Dealer One",
            Email = " dealer@example.com ",
            PhoneNumber = "0400000000",
            Password = "P@ssword123!"
        };
        var dealer = Dealer.CreateDealer("Dealer One", "dealer@example.com", "0400000000", "P@ssword123!", _passwordHasherMock.Object);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealer);

        // Act
        var act = async () => await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ex.Which.Message.Should().Be("Email already exists");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
        _jwtTokenServiceMock.Verify(
            x => x.GenerateToken(It.IsAny<Dealer>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailIsValid_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "Dealer One",
            Email = " dealer@example.com ",
            PhoneNumber = "0400000000",
            Password = "P@ssword123!"
        };
        Dealer? savedDealer = null;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(60);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        _passwordHasherMock
            .Setup(x => x.HashPassword(It.IsAny<Dealer>(), "P@ssword123!"))
            .Returns("hashed-password");

        _dealerRepositoryMock
            .Setup(x => x.AddDealerAsync(It.IsAny<Dealer>(), It.IsAny<CancellationToken>()))
            .Callback<Dealer, CancellationToken>((dealer, _) => savedDealer = dealer)
            .Returns(Task.CompletedTask);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<Dealer>()))
            .Returns(("fake-jwt-token", expiresAtUtc));

        // Act
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Dealer One");
        result.Email.Should().Be("dealer@example.com");
        result.AccessToken.Should().Be("fake-jwt-token");
        result.ExpiresAtUtc.Should().Be(expiresAtUtc);

        savedDealer.Should().NotBeNull();
        savedDealer.Name.Should().Be("Dealer One");
        savedDealer.Email.Should().Be("dealer@example.com");
        savedDealer.PasswordHash.Should().NotBeNullOrWhiteSpace();

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByEmailAsync("dealer@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
        _passwordHasherMock.Verify(
            x => x.HashPassword(It.IsAny<Dealer>(), "P@ssword123!"),
            Times.Once);
        _dealerRepositoryMock.Verify(
            x => x.AddDealerAsync(It.IsAny<Dealer>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _jwtTokenServiceMock.Verify(
            x => x.GenerateToken(It.IsAny<Dealer>()),
            Times.Once);
    }
}