using CarManagement.DataAccess.Data;
using CarManagement.Repository.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;

namespace CarManagement.Repository.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public UnitOfWork(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Gets the current database connection.
    /// Throws if the transaction has not been started yet.
    /// </summary>
    public IDbConnection Connection => _connection
        ?? throw new InvalidOperationException("Connection has not been initialized.");

    /// <summary>
    /// Gets the current database transaction.
    /// </summary>
    public IDbTransaction? Transaction => _transaction;

    /// <summary>
    /// Opens the connection and begins a transaction.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        if (_connection is not null || _transaction is not null)
        {
            throw new InvalidOperationException("A transaction has already been started.");
        }

        _connection = await _connectionFactory.CreateConnectionAsync(ct);
        _transaction = await _connection.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public async Task CommitAsync(CancellationToken ct)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        await _transaction.CommitAsync(ct);
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public async Task RollbackAsync(CancellationToken ct)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(ct);
    }

    /// <summary>
    /// Closes the connection and transaction.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <summary>
    /// Closes the connection and transaction synchronously.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;

        _connection?.Dispose();
        _connection = null;
    }
}
