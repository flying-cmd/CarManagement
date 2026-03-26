using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagementApi.Repository.Interfaces;
using Dapper;

namespace CarManagement.Repository.Repositories;

public class DealerRepository : IDealerRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DealerRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Add new dealer.
    /// </summary>
    /// <param name="dealer">The dealer entity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    public async Task AddDealerAsync(Dealer dealer, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Dealers (Id, Name, Email, PhoneNumber, PasswordHash, CreatedAt)
            VALUES (@Id, @Name, @Email, @PhoneNumber, @PasswordHash, @CreatedAt)
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = dealer.Id.ToString(),
                Name = dealer.Name,
                Email = dealer.Email,
                PhoneNumber = dealer.PhoneNumber,
                PasswordHash = dealer.PasswordHash,
                CreatedAt = dealer.CreatedAt.ToString("O")
            },
            cancellationToken: ct));
    }

    /// <summary>
    /// Get dealer by email.
    /// </summary>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns the dealer if found, otherwise null.</returns>
    public async Task<Dealer?> GetDealerByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Dealers WHERE Email = @email";

        using var connection = _connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<DealerRow>(
            new CommandDefinition(sql, new { email }, cancellationToken: ct));

        if (row is null)
        {
            return null;
        }

        return Dealer.Rehydrate(
            Guid.Parse(row.Id),
            row.Name,
            row.Email,
            row.PhoneNumber,
            row.PasswordHash,
            DateTimeOffset.Parse(row.CreatedAt));
    }

    /// <summary>
    /// Get dealer by id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns the dealer if found, otherwise null.</returns>
    public async Task<Dealer?> GetDealerByIdAsync(Guid dealerId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Dealers WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<DealerRow>(
            new CommandDefinition(sql, new { Id = dealerId.ToString() }, cancellationToken: ct));

        if (row is null)
        {
            return null;
        }

        return Dealer.Rehydrate(
            Guid.Parse(row.Id),
            row.Name,
            row.Email,
            row.PhoneNumber,
            row.PasswordHash,
            DateTimeOffset.Parse(row.CreatedAt));
    }

    /// <summary>
    /// Dealer row. Returns by the SQL query.
    /// </summary>
    private sealed class DealerRow
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string CreatedAt { get; set; } = null!;
    }
}
