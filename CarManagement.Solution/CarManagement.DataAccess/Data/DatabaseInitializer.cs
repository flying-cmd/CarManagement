using CarManagement.Models.Entities;
using Dapper;
using Microsoft.AspNetCore.Identity;

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
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Cars (
                Id TEXT PRIMARY KEY,
                DealerId TEXT NOT NULL,
                Make TEXT NOT NULL COLLATE NOCASE,
                Model TEXT NOT NULL COLLATE NOCASE,
                Year INTEGER NOT NULL,
                Color TEXT NOT NULL COLLATE NOCASE,
                Price REAL NOT NULL CHECK (Price >= 0),
                StockLevel INTEGER NOT NULL CHECK (StockLevel >= 0),
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (DealerId) REFERENCES Dealers (Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Cars_DealerId_Make_Model_Year
            ON Cars (DealerId, Make, Model, Year);
            """;

        // Create the schema
        await connection.ExecuteAsync(new CommandDefinition(schemaSql, cancellationToken: cancellationToken));

        // Seed dealers data
        await SeedDealerAsync(connection, "tom", "tom@example.com", "Pass123$", cancellationToken);
        await SeedDealerAsync(connection, "jack", "jack@example.com", "Pass123$", cancellationToken);
    }

    /// <summary>
    /// Seed dealers
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="name">The name of the dealer.</param>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="password">The password of the dealer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task SeedDealerAsync(System.Data.IDbConnection connection, string name, string email, string password, CancellationToken cancellationToken = default)
    {
        // Check if the dealer already exists
        var exists = await connection.QuerySingleOrDefaultAsync<Dealer>(
            new CommandDefinition(
                "SELECT * FROM Dealers WHERE Email = @email",
                new { email }, cancellationToken: cancellationToken));

        if (exists is not null)
        {
            return;
        }

        // If the dealer doesn't exist, create it
        var dealer = Dealer.CreateDealer(name, email, password, _passwordHasher);

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Dealers (Id, Name, Email, PasswordHash, CreatedAt) VALUES (@Id, @Name, @Email, @PasswordHash, @CreatedAt)",
            new { Id = dealer.Id, Name = dealer.Name, Email = dealer.Email, PasswordHash = dealer.PasswordHash, CreatedAt = dealer.CreatedAt }, cancellationToken: cancellationToken));
    }
}
