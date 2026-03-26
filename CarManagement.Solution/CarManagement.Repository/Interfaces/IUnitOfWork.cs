using System.Data;

namespace CarManagement.Repository.Interfaces;

/// <summary>
/// Coordinates database transactions for a single unit of work.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// The database connection.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// The database transaction.
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// Begin a database transaction.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken ct);

    /// <summary>
    /// Commit a database transaction.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task CommitAsync(CancellationToken ct);

    /// <summary>
    /// Rollback a database transaction.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    Task RollbackAsync(CancellationToken ct);
}
