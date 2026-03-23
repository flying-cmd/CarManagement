using CarManagement.Models.Entities;

namespace CarManagementApi.Repository.Interfaces;

public interface IDealerRepository
{
    Task<Dealer?> GetDealerByEmailAsync(string email, CancellationToken ct);
}
