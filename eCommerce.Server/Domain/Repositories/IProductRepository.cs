using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Domain.Entities;

namespace eCommerce.Server.Domain.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetActivePagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetActiveByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetActiveBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithImagesAsync(int productId, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Remove(Product product);
    void RemoveImage(ProductImage image);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
