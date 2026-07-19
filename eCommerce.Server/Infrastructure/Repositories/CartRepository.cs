using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Domain.Repositories;
using eCommerce.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Server.Infrastructure.Repositories;

public class CartRepository(AppDbContext db) : ICartRepository
{
    public Task<Cart?> GetByUserIdWithItemsAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Carts
          .Include(c => c.Items)
              .ThenInclude(i => i.Product)
                  .ThenInclude(p => p.Images)
          .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task AddAsync(Cart cart, CancellationToken cancellationToken = default) =>
        db.Carts.AddAsync(cart, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
