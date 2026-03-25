using CarManagement.Common.Helpers;
using CarManagement.IntegrationTests.Infrastructure;
using CarManagement.Service.DTOs.Auth;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CarManagement.IntegrationTests.Api;

public sealed class AuthApiIntegrationTests : IClassFixture<CarManagementWebApplicationFactory>
{
    private readonly CarManagementWebApplicationFactory _factory;

    public AuthApiIntegrationTests(CarManagementWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WhenRequestIsInvalid_ShouldReturnValidationErrors()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new RegisterRequestDto
        {
            Name = "",
            Email = "bad-email",
            Password = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().NotBeNullOrWhiteSpace();
        body.Errors.Should().NotBeNull();
        body.Errors!.Should().ContainKey("name");
        body.Errors.Should().ContainKey("email");
        body.Errors.Should().ContainKey("password");
        body.Errors["name"].Should().Contain("Name is required");
        body.Errors["email"].Should().Contain("Email is invalid");
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldCreateDealerAndReturnAccessToken()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new RegisterRequestDto
        {
            Name = "DealerOne",
            Email = "dealer@example.com",
            Password = "Pass123$"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Message.Should().Be("Registration successfully");
        body.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be(request.Name);
        body.Data.Email.Should().Be(request.Email);
        body.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ShouldReturnUnauthorized()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var request = new LoginRequestDto
        {
            Email = dealer.Email,
            Password = "WrongPass123$"
        };

        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Invalid email or password");
        body.Errors.Should().NotBeNull();
        body.Errors!["Error"].Should().ContainSingle("Invalid email or password");
    }

    [Fact]
    public async Task Login_WhenEmailIsWrong_ShouldReturnUnauthorized()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var request = new LoginRequestDto
        {
            Email = "dealer2@example.com",
            Password = dealer.Password
        };

        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Invalid email or password");
        body.Errors.Should().NotBeNull();
        body.Errors!["Error"].Should().ContainSingle("Invalid email or password");
    }

    [Fact]
    public async Task Login_WhenEmailAndPasswordAreValid_ShouldReturnAccessToken()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var request = new LoginRequestDto
        {
            Email = dealer.Email,
            Password = dealer.Password
        };

        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Message.Should().Be("Login successfully");
        body.Data.Should().NotBeNull();
        body.Data!.Email.Should().Be(dealer.Email);
        body.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}
