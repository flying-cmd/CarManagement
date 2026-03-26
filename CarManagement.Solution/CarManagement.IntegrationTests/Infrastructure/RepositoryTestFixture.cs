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

    /// <summary>
    /// Initialize the test database.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        var options = Options.Create(new DatabaseOptions { FilePath = _databasePath });
        var environment = new TestWebHostEnvironment();
        var connectionFactory = new SqliteConnectionFactory(options, environment);
        var initializer = new DatabaseInitializer(connectionFactory, PasswordHasher);

        await initializer.InitializeAsync();

        DealerRepository = new DealerRepository(connectionFactory);
        CarRepository = new CarRepository(connectionFactory);
    }

    /// <summary>
    /// Create a new dealer.
    /// </summary>
    public async Task<Dealer> CreateDealerAsync(
        string? name = null,
        string? email = null,
        string? phoneNumber = null,
        string password = "Pass123$")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dealer = Dealer.CreateDealer(
            name ?? $"Dealer{suffix}",
            email ?? $"dealer.{suffix}@example.com",
            phoneNumber ?? $"04{Random.Shared.Next(0, 100000000):D8}",
            password,
            PasswordHasher);

        await DealerRepository.AddDealerAsync(dealer, CancellationToken.None);

        return dealer;
    }

    /// <summary>
    /// Create a new car.
    /// </summary>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <returns>Returns the created car <see cref="Car"/></returns>
    public async Task<Car> CreateCarAsync(
        string? make = null,
        string? model = null,
        int year = 2024)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var car = new Car(
            make ?? $"Make{suffix}",
            model ?? $"Model{suffix}",
            year);

        await CarRepository.AddCarAsync(car, CancellationToken.None);

        return car;
    }

    public async Task<CarStock> CreateCarStockAsync(
        Guid dealerId,
        Guid carId,
        int stockLevel = 3,
        decimal unitPrice = 25000m)
    {
        var carStock = new CarStock(dealerId, carId, stockLevel, unitPrice);

        await CarRepository.AddCarStockAsync(carStock, CancellationToken.None);

        return carStock;
    }

    public async Task DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await DeleteDirectoryWithRetryAsync(_databaseDirectory);
    }

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
