using CarManagement.Common.Helpers;
using CarManagement.Service.DTOs.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace CarManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Custom web application factory for integration tests.
/// </summary>
public sealed class CarManagementWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseDirectory = Path.Combine(Path.GetTempPath(), $"car-management-api-tests-{Guid.NewGuid():N}");
    private readonly string _databasePath;
    // A thread-safe flag to prevent multiple cleanups
    private int _disposed;

    public CarManagementWebApplicationFactory()
    {
        // Build the database file path
        _databasePath = Path.Combine(_databaseDirectory, "CarManagement.db");
        Directory.CreateDirectory(_databaseDirectory);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:FilePath"] = _databasePath
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Create an HttpClient with the specified access token.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <returns>Returns an HttpClient with the specified access token.</returns>
    public HttpClient CreateAuthorizedClient(string accessToken)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        return client;
    }

    /// <summary>
    /// Registers a new dealer.
    /// </summary>
    /// <param name="name">The name of the dealer.</param>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="password">The password of the dealer.</param>
    /// <returns>Returns the registered dealer.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the registration response does not contain auth data.</exception>
    public async Task<RegisteredDealer> RegisterDealerAsync(
        string? name = null,
        string? email = null,
        string? password = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterRequestDto
        {
            Name = name ?? $"Dealer{suffix}",
            Email = email ?? $"dealer.{suffix}@example.com",
            Password = password ?? "Pass123$"
        };

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        if (body?.Data is null)
        {
            throw new InvalidOperationException("Registration response did not contain auth data.");
        }

        return new RegisteredDealer(
            request.Name,
            request.Email,
            request.Password,
            body.Data.AccessToken);
    }

    Task IAsyncLifetime.DisposeAsync() => CleanupAsync();

    /// <summary>
    /// Cleans up the test environment.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    private async Task CleanupAsync()
    {
        // Mark the object as disposed
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        // Dispose the WebApplicationFactory
        await base.DisposeAsync();

        // Delete the test database file
        await DeleteDirectoryWithRetryAsync(_databaseDirectory);
    }

    /// <summary>
    /// Deletes a directory with retries.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    private static async Task DeleteDirectoryWithRetryAsync(string path)
    {
        // If the directory does not exist, return directly
        if (!Directory.Exists(path))
        {
            return;
        }

        // Retry deleting the directory
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                // Delete the folder and all its contents
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException)
            {
                if (attempt == 10)
                {
                    return;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(250 * attempt);
            }
            catch (UnauthorizedAccessException)
            {
                if (attempt == 10)
                {
                    return;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(250 * attempt);
            }
        }
    }
}

/// <summary>
/// Represents a registered dealer.
/// </summary>
/// <param name="Name">The name of the dealer.</param>
/// <param name="Email">The email of the dealer.</param>
/// <param name="Password">The password of the dealer.</param>
/// <param name="AccessToken">The access token of the dealer.</param>
public sealed record RegisteredDealer(
    string Name,
    string Email,
    string Password,
    string AccessToken);
