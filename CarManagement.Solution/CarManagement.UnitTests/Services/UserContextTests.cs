using CarManagement.Common.Exceptions;
using CarManagement.Service.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarManagement.UnitTests.Services;

public class UserContextTests
{
    private static DefaultHttpContext BuildHttpContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }

    [Fact]
    public void DealerId_WhenHttpContextIsNull_ShouldThrowUnzuthorized()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = null
        };

        var sut = new UserContext(httpContextAccessor);

        // Act
        var act = () => sut.DealerId;

        // Assert
        var ex = act.Should().Throw<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("No HttpContext available.");
    }

    [Fact]
    public void DealerId_WhenUserIdIsMissing_ShouldThrowUnzuthorized()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = BuildHttpContext()
        };

        var sut = new UserContext(httpContextAccessor);

        // Act
        var act = () => sut.DealerId;

        // Assert
        var ex = act.Should().Throw<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Invalid user id.");
    }

    [Fact]
    public void DealerId_WhenUserIdIsNotAGuid_ShouldThrowUnzuthorized()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = BuildHttpContext(new Claim("UserId", "not-a-guid"))
        };

        var sut = new UserContext(httpContextAccessor);

        // Act
        var act = () => sut.DealerId;

        // Assert
        var ex = act.Should().Throw<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Invalid user id.");
    }

    [Fact]
    public void DealerId_WhenUserIdIsValid_ShouldReturnUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = BuildHttpContext(new Claim("UserId", userId.ToString()))
        };

        var sut = new UserContext(httpContextAccessor);

        // Act
        var result = sut.DealerId;

        // Assert
        result.Should().Be(userId);
    }
}
