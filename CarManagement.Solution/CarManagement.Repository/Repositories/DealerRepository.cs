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
