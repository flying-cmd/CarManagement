using CarManagement.Common.Helpers;
using CarManagement.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarManagement.Service.Services;

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
