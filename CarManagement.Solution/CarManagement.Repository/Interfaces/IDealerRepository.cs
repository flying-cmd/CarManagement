using CarManagement.Models.Entities;
using System.Data;

namespace CarManagementApi.Repository.Interfaces;

public interface IDealerRepository
{
    /// <summary>
    /// Add new dealer.
    /// </summary>
    /// <param name="dealer">The dealer entity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task AddDealerAsync(Dealer dealer, CancellationToken ct);

    /// <summary>
    /// Get dealer by email.
    /// </summary>
    /// <param name="email">The email of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns the <see cref="Dealer"/> if found, otherwise null.</returns>
    Task<Dealer?> GetDealerByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Get dealer by id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="dbConnection">Optional. The database connection.</param>
    /// <param name="dbTransaction">Optional. The database transaction.</param>
    /// <returns>Returns the <see cref="Dealer"/> if found, otherwise null.</returns>
    Task<Dealer?> GetDealerByIdAsync(Guid dealerId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null);
}
