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
        DatabasePath = Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(environment.ContentRootPath, configuredPath);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath
        }.ToString();
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable foreign key constraints
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON";
        command.ExecuteNonQuery();

        return connection;
    }
}
