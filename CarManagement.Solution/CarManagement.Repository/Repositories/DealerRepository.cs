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
            INSERT INTO Dealers (Id, Name, Email, PasswordHash, CreatedAt) 
            VALUES (@Id, @Name, @Email, @PasswordHash, @CreatedAt)
            """;

        using var connection = _connectionFactory.CreateConnection();
        
        await connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new
            {
                Id = dealer.Id.ToString(),
                Name = dealer.Name,
                Email = dealer.Email,
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
            row.PasswordHash,
            DateTimeOffset.Parse(row.CreatedAt));
    }

    /// <summary>
    /// Dealer row. Returns by the SQL query.
    /// </summary>
    private sealed class DealerRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
