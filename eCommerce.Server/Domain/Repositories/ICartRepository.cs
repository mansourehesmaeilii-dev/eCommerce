using eCommerce.Server.Domain.Entities;

namespace eCommerce.Server.Domain.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdWithItemsAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
