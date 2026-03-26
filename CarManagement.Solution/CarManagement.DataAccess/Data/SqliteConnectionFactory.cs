using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace CarManagement.DataAccess.Data;

// This class is a factory responsible for creating a connection to the database
public class SqliteConnectionFactory
{
    public string DatabasePath;
    private readonly string _connectionString;

    public SqliteConnectionFactory(IOptions<DatabaseOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.FilePath;

        // Build the absolute path to the db file
        var parentFolder = Directory.GetParent(environment.ContentRootPath)?.FullName ?? throw new InvalidOperationException("Unable to get parent folder.");

        DatabasePath = Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(parentFolder, configuredPath);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath
        }.ToString();
    }

    /// <summary>
    /// Creates and opens a SQLite connection asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async Task<SqliteConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Enable foreign key constraints
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON";
        await command.ExecuteNonQueryAsync();

        return connection;
    }
}
