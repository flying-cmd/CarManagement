using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using NSwag;

namespace CarManagement.Common.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>Returns the added service collection.</returns>
    public static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.SwaggerDocument(o =>
        {
            o.EnableJWTBearerAuth = false;
            o.DocumentSettings = s =>
            {
                s.Title = "Car Management API";
                s.Version = "v1";
                s.AddAuth("Bearer", new()
                {
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    Description = "Enter the JWT token."
                });
            };
        });

        return services;
    }
}
