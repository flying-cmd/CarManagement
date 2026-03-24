using CarManagement.Common.Exceptions;
using CarManagement.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarManagement.Service.Services;

/// <summary>
/// Gets the dealer id of the currently authenticated user from the HTTP context claims.
/// </summary>
/// <exception cref="ApiException.Unauthorized(string)">Thrown when no HTTP context is available, or when the user id claim is missing or invalid.</exception>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the dealer id of the current user.
    /// </summary>
    public Guid DealerId
    {
        get
        {
            if (_httpContextAccessor.HttpContext is null)
            {
                throw ApiException.Unauthorized("No HttpContext available.");
            }

            var claimValue = _httpContextAccessor.HttpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var dealerId))
            {
                throw ApiException.Unauthorized("Invalid user id.");
            }

            return dealerId;
        }
    }
}
