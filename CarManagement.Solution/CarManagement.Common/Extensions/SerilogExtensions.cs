using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CarManagement.Common.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging in the WebApplicationBuilder.
/// Provides helper methods to set up structured logging with Serilog and log application startup events.
/// </summary>
public static class LoggingWebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures and adds Serilog logging to the application.
    /// This method sets up Serilog with structured logging configuration,
    /// including reading settings from appsettings.json and enriching log entries
    /// with contextual information such as application name, version, and environment.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to configure logging for.</param>
    /// <returns>The WebApplicationBuilder instance for method chaining.</returns>
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration) // Read settings from appsettings.json
                .Enrich.FromLogContext() // Enable log context enrichment
                .Enrich.WithProperty("Application", context.Configuration["ApplicationInfo:Name"] ?? "Car Management API")
                .Enrich.WithProperty("Version", context.Configuration["ApplicationInfo:Version"] ?? "Unknown")
                .Enrich.WithProperty("Environment", context.Configuration["ApplicationInfo:Environment"] ?? context.HostingEnvironment.EnvironmentName);
        });

        return builder;
    }

    /// <summary>
    /// Logs the application startup information to indicate successful initialization.
    /// This method retrieves the logger from the dependency injection container and writes
    /// a series of informational log messages including the current environment name.
    /// </summary>
    /// <param name="app">The WebApplication instance whose startup information should be logged.</param>
    public static void LogApplicationStartup(this WebApplication app)
    {
        // Get the logging Factory from the DI container
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        // Creates a logger with category name "Application" which is used for filtering
        var logger = loggerFactory.CreateLogger("Application");
        logger.LogInformation("========================================");
        logger.LogInformation("Application started successfully");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
        logger.LogInformation("========================================");
    }
}
