using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace CarManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Set up a SQLite test database for integration tests.
/// It initializes the schema, create repository instance, 
/// seed test data and clean up the test database files after the test.
/// </summary>
public sealed class RepositoryTestFixture : IAsyncLifetime
{
    // Temporary directory for the test database
    private readonly string _databaseDirectory = Path.Combine(Path.GetTempPath(), $"car-management-repository-tests-{Guid.NewGuid():N}");

    private readonly string _databasePath;
    // A thread-safe flag to prevent multiple cleanups
    private int _disposed;

    public PasswordHasher<Dealer> PasswordHasher { get; } = new();
    public DealerRepository DealerRepository { get; private set; } = null!;
    public CarRepository CarRepository { get; private set; } = null!;

    public RepositoryTestFixture()
    {
        // Build the database file path
        _databasePath = Path.Combine(_databaseDirectory, "CarManagement.db");
        Directory.CreateDirectory(_databaseDirectory);
    }

    public async Task InitializeAsync()
    {
        var options = Options.Create(new DatabaseOptions
        {
            FilePath = _databasePath
        });

        var environment = new TestWebHostEnvironment();
        var connectionFactory = new SqliteConnectionFactory(options, environment);
        var initializer = new DatabaseInitializer(connectionFactory, PasswordHasher);

        await initializer.InitializeAsync();

        DealerRepository = new DealerRepository(connectionFactory);
        CarRepository = new CarRepository(connectionFactory);
    }

    /// <summary>
    /// Helper method to create a new dealer.
    /// </summary>
    /// <returns></returns>
    public async Task<Dealer> CreateDealerAsync()
    {
        var dealer = Dealer.CreateDealer(
            name: $"Dealer{Guid.NewGuid():N}"[..14],
            email: $"dealer.{Guid.NewGuid():N}@example.com",
            plainPassword: "Pass123$",
            passwordHasher: PasswordHasher);

        await DealerRepository.AddDealerAsync(dealer, CancellationToken.None);

        return dealer;
    }

    /// <summary>
    /// Helper method to clean up the test database files.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await DeleteDirectoryWithRetryAsync(_databaseDirectory);
    }

    /// <summary>
    /// Helper method to delete a directory with retries.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    private static async Task DeleteDirectoryWithRetryAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
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
