using CarManagement.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace CarManagement.DataAccess.Data;

public class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _sqliteConnectionFactory;
    private readonly IPasswordHasher<Dealer> _passwordHasher;

    public DatabaseInitializer(SqliteConnectionFactory sqliteConnectionFactory, IPasswordHasher<Dealer> passwordHasher)
    {
        _sqliteConnectionFactory = sqliteConnectionFactory;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Initialize the database
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Create the directory if it doesn't exist
        var directory = Path.GetDirectoryName(_sqliteConnectionFactory.DatabasePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = await _sqliteConnectionFactory.CreateConnectionAsync();

        // Create the schema
        var schemaSql = """
            CREATE TABLE IF NOT EXISTS Dealers (
                Id TEXT PRIMARY KEY COLLATE NOCASE,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PhoneNumber TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Cars (
                Id TEXT PRIMARY KEY COLLATE NOCASE,
                Make TEXT NOT NULL COLLATE NOCASE,
                Model TEXT NOT NULL COLLATE NOCASE,
                Year INTEGER NOT NULL CHECK (Year > 0),
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CarStocks (
                Id TEXT PRIMARY KEY COLLATE NOCASE,
                DealerId TEXT NOT NULL,
                CarId TEXT NOT NULL,
                StockLevel INTEGER NOT NULL CHECK (StockLevel >= 0),
                UnitPrice REAL NOT NULL CHECK (UnitPrice >= 0),
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (DealerId) REFERENCES Dealers(Id) ON DELETE CASCADE,
                FOREIGN KEY (CarId) REFERENCES Cars(Id) ON DELETE CASCADE,
                CONSTRAINT UX_CarStocks_DealerId_CarId UNIQUE (DealerId, CarId)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS UX_Cars_Make_Model_Year
            ON Cars (Make, Model, Year);

            CREATE INDEX IF NOT EXISTS IX_Cars_Make_Model_Year
            ON Cars (Make, Model, Year);

            CREATE INDEX IF NOT EXISTS IX_CarStocks_DealerId
            ON CarStocks (DealerId);

            CREATE INDEX IF NOT EXISTS IX_CarStocks_CarId
            ON CarStocks (CarId);
            """;

        // Create the schema
        await connection.ExecuteAsync(new CommandDefinition(schemaSql, cancellationToken: cancellationToken));

        // Seed data
        using var transaction = await connection.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var tomDealerId = await SeedDealerAsync(
                connection,
                transaction,
                "Tom Dealer",
                "tom@example.com",
                "0412345678",
                "Pass123$",
                cancellationToken);

            var jackDealerId = await SeedDealerAsync(
                connection,
                transaction,
                "Jack Dealer",
                "jack@example.com",
                "0498765432",
                "Pass123$",
                cancellationToken);

            var corollaId = await SeedCarAsync(
                connection,
                transaction,
                "Toyota",
                "Corolla",
                2022,
                cancellationToken);

            var mazda3Id = await SeedCarAsync(
                connection,
                transaction,
                "Mazda",
                "Mazda3",
                2023,
                cancellationToken);

            var civicId = await SeedCarAsync(
                connection,
                transaction,
                "Honda",
                "Civic",
                2021,
                cancellationToken);

            await SeedCarStockAsync(
                connection,
                transaction,
                tomDealerId,
                corollaId,
                5,
                28990m,
                cancellationToken);

            await SeedCarStockAsync(
                connection,
                transaction,
                tomDealerId,
                mazda3Id,
                3,
                31990m,
                cancellationToken);

            await SeedCarStockAsync(
                connection,
                transaction,
                jackDealerId,
                corollaId,
                2,
                29500m,
                cancellationToken);

            await SeedCarStockAsync(
                connection,
                transaction,
                jackDealerId,
                civicId,
                4,
                27990m,
                cancellationToken);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Seed a dealer if it does not already exist.
    /// </summary>
    /// <param name="connection">The DB connection.</param>
    /// <param name="transaction">The DB transaction.</param>
    /// <param name="name">The name of the dealer.</param>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="phoneNumber">The phone number of the dealer.</param>
    /// <param name="password">The password of the dealer.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Return the dealer id.</returns>
    private async Task<Guid> SeedDealerAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string name,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var trimmedPhoneNumber = phoneNumber.Trim();

        // Check if the dealer already exists
        var existingDealerId = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT Id
                FROM Dealers
                WHERE Email = @Email
                LIMIT 1;
                """,
                new { Email = normalizedEmail },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // If the dealer already exists, return the id
        if (!string.IsNullOrWhiteSpace(existingDealerId))
        {
            return Guid.Parse(existingDealerId);
        }

        // Create new dealer
        var dealer = Dealer.CreateDealer(
            trimmedName,
            normalizedEmail,
            trimmedPhoneNumber,
            password,
            _passwordHasher);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO Dealers (Id, Name, Email, PhoneNumber, PasswordHash, CreatedAt)
                VALUES (@Id, @Name, @Email, @PhoneNumber, @PasswordHash, @CreatedAt);
                """,
                new
                {
                    Id = dealer.Id.ToString(),
                    dealer.Name,
                    dealer.Email,
                    dealer.PhoneNumber,
                    dealer.PasswordHash,
                    CreatedAt = dealer.CreatedAt.ToString("O")
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        return dealer.Id;
    }

    /// <summary>
    /// Seed a car if it does not already exist.
    /// </summary>
    /// <param name="connection">The DB connection.</param>
    /// <param name="transaction">The DB transaction.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns the car id.</returns>
    private async Task<Guid> SeedCarAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string make,
        string model,
        int year,
        CancellationToken cancellationToken = default)
    {
        var trimmedMake = make.Trim();
        var trimmedModel = model.Trim();

        // Check if the car already exists
        var existingCarId = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT Id
                FROM Cars
                WHERE Make = @Make
                  AND Model = @Model
                  AND Year = @Year
                LIMIT 1;
                """,
                new
                {
                    Make = trimmedMake,
                    Model = trimmedModel,
                    Year = year
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // If the car already exists, return the id
        if (!string.IsNullOrWhiteSpace(existingCarId))
        {
            return Guid.Parse(existingCarId);
        }

        // Create new car
        var car = new Car(trimmedMake, trimmedModel, year);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO Cars (Id, Make, Model, Year, CreatedAt)
                VALUES (@Id, @Make, @Model, @Year, @CreatedAt);
                """,
                new
                {
                    Id = car.Id.ToString(),
                    car.Make,
                    car.Model,
                    car.Year,
                    CreatedAt = car.CreatedAt.ToString("O")
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        return car.Id;
    }

    /// <summary>
    /// Seed a car stock if it does not already exist.
    /// </summary>
    /// <param name="connection">The DB connection.</param>
    /// <param name="transaction">The DB transaction.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="carId">The id of the car.</param>
    /// <param name="stockLevel">The stock level of the car.</param>
    /// <param name="unitPrice">The unit price of the car.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    private async Task SeedCarStockAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid dealerId,
        Guid carId,
        int stockLevel,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        // Check if the car stock already exists
        var existingStockId = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT Id
                FROM CarStocks
                WHERE DealerId = @DealerId
                  AND CarId = @CarId
                LIMIT 1;
                """,
                new
                {
                    DealerId = dealerId.ToString(),
                    CarId = carId.ToString()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // If the car stock already exists, return directly
        if (!string.IsNullOrWhiteSpace(existingStockId))
        {
            return;
        }

        // Create new car stock
        var carStock = new CarStock(dealerId, carId, stockLevel, unitPrice);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO CarStocks (Id, DealerId, CarId, StockLevel, UnitPrice, UpdatedAt)
                VALUES (@Id, @DealerId, @CarId, @StockLevel, @UnitPrice, @UpdatedAt);
                """,
                new
                {
                    Id = carStock.Id.ToString(),
                    DealerId = carStock.DealerId.ToString(),
                    CarId = carStock.CarId.ToString(),
                    carStock.StockLevel,
                    carStock.UnitPrice,
                    UpdatedAt = carStock.UpdatedAt.ToString("O")
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }
}
