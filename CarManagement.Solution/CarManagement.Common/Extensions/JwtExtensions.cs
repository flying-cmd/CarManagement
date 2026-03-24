using CarManagement.Common.Constants;
using FastEndpoints.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarManagement.Common.Extensions;

public static class JwtExtensions
{
    /// <summary>
    /// Adds JWT authentication and authorization to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The added service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the JWT signing key is not set.</exception>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("The JWT signing key is not set.");
        }

        services.AddAuthenticationJwtBearer(s => s.SigningKey = signingKey);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("DealerOnly", x => x.RequireRole(RoleNames.DEALER));
        });

        return services;
    }
}
