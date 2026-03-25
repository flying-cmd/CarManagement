using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CarManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Test web host environment for integration tests.
/// Create a controlled environment for integration tests.
/// </summary>
public sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    // Application assembly name
    public string ApplicationName { get; set; } = "CarManagement.IntegrationTests";
    // File Access for web root
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    // Phisical path to web root
    public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    public string EnvironmentName { get; set; } = Environments.Development;
    // Phisical path to app's content files, used to get database file
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    // File Provider for content root
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
