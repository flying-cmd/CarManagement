using CarManagement.Models.Entities;

namespace CarManagementApi.Repository.Interfaces;

public interface IDealerRepository
{
    Task AddDealerAsync(Dealer dealer, CancellationToken ct);
    Task<Dealer?> GetDealerByEmailAsync(string email, CancellationToken ct);
    Task<Dealer?> GetDealerByIdAsync(Guid dealerId, CancellationToken ct);
}
