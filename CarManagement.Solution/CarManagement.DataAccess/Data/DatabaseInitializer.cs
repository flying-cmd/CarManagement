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

        using var connection = _sqliteConnectionFactory.CreateConnection();

        var schemaSql = """
            CREATE TABLE IF NOT EXISTS Dealers (
                Id TEXT PRIMARY KEY COLLATE NOCASE,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PhoneNumber TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DealerAddresses (
                Id TEXT PRIMARY KEY COLLATE NOCASE,
                DealerId TEXT NOT NULL,
                Line TEXT NOT NULL,
                Suburb TEXT NOT NULL COLLATE NOCASE,
                State TEXT NOT NULL COLLATE NOCASE,
                Postcode TEXT NOT NULL COLLATE NOCASE,
                Country TEXT NOT NULL COLLATE NOCASE,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (DealerId) REFERENCES Dealers(Id) ON DELETE CASCADE
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

            CREATE INDEX IF NOT EXISTS IX_DealerAddresses_DealerId
            ON DealerAddresses (DealerId);

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
                "tom",
                "tom@example.com",
                "0412345678",
                "Pass123$",
                cancellationToken);

            var jackDealerId = await SeedDealerAsync(
                connection,
                transaction,
                "jack",
                "jack@example.com",
                "0498765432",
                "Pass123$",
                cancellationToken);

            await SeedDealerAddressAsync(
                connection,
                transaction,
                tomDealerId,
                "100 Collins Street",
                "Melbourne",
                "VIC",
                "3000",
                "Australia",
                cancellationToken);

            await SeedDealerAddressAsync(
                connection,
                transaction,
                tomDealerId,
                "25 Logistics Drive",
                "Dandenong South",
                "VIC",
                "3175",
                "Australia",
                cancellationToken);

            await SeedDealerAddressAsync(
                connection,
                transaction,
                jackDealerId,
                "200 George Street",
                "Sydney",
                "NSW",
                "2000",
                "Australia",
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
    /// Returns the dealer id.
    /// </summary>
    private async Task<Guid> SeedDealerAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string name,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

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

        if (!string.IsNullOrWhiteSpace(existingDealerId))
        {
            return Guid.Parse(existingDealerId);
        }

        var dealer = Dealer.CreateDealer(
            name.Trim(),
            normalizedEmail,
            phoneNumber.Trim(),
            password,
            _passwordHasher);

        await connection.ExecuteAsync(new CommandDefinition(
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
    /// Seed a dealer address if it does not already exist.
    /// </summary>
    private async Task SeedDealerAddressAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid dealerId,
        string line,
        string suburb,
        string state,
        string postcode,
        string country,
        CancellationToken cancellationToken = default)
    {
        var existingAddressId = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT Id
                FROM DealerAddresses
                WHERE DealerId = @DealerId
                  AND Line = @Line
                  AND Suburb = @Suburb
                  AND State = @State
                  AND Postcode = @Postcode
                  AND Country = @Country
                LIMIT 1;
                """,
                new
                {
                    DealerId = dealerId.ToString(),
                    Line = line.Trim(),
                    Suburb = suburb.Trim(),
                    State = state.Trim(),
                    Postcode = postcode.Trim(),
                    Country = country.Trim()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!string.IsNullOrWhiteSpace(existingAddressId))
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO DealerAddresses (Id, DealerId, Line, Suburb, State, Postcode, Country, CreatedAt)
            VALUES (@Id, @DealerId, @Line, @Suburb, @State, @Postcode, @Country, @CreatedAt);
            """,
            new
            {
                Id = Guid.NewGuid().ToString(),
                DealerId = dealerId.ToString(),
                Line = line.Trim(),
                Suburb = suburb.Trim(),
                State = state.Trim(),
                Postcode = postcode.Trim(),
                Country = country.Trim(),
                CreatedAt = DateTimeOffset.UtcNow.ToString("O")
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Seed a car if it does not already exist.
    /// Returns the car id.
    /// </summary>
    private async Task<Guid> SeedCarAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string make,
        string model,
        int year,
        CancellationToken cancellationToken = default)
    {
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
                    Make = make.Trim(),
                    Model = model.Trim(),
                    Year = year
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!string.IsNullOrWhiteSpace(existingCarId))
        {
            return Guid.Parse(existingCarId);
        }

        var car = new Car(make.Trim(), model.Trim(), year);

        await connection.ExecuteAsync(new CommandDefinition(
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
    /// If it exists, update stock level, unit price and updated time.
    /// </summary>
    private async Task SeedCarStockAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid dealerId,
        Guid carId,
        int stockLevel,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var existingStockId = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT Id
                FROM CarStocks
                WHERE DealerId = @DealerId AND CarId = @CarId
                LIMIT 1;
                """,
                new
                {
                    DealerId = dealerId.ToString(),
                    CarId = carId.ToString()
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!string.IsNullOrWhiteSpace(existingStockId))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE CarStocks
                SET StockLevel = @StockLevel,
                    UnitPrice = @UnitPrice,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """,
                new
                {
                    Id = existingStockId,
                    StockLevel = stockLevel,
                    UnitPrice = unitPrice,
                    UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            return;
        }

        var carStock = new CarStock(dealerId, carId, stockLevel, unitPrice);

        await connection.ExecuteAsync(new CommandDefinition(
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
