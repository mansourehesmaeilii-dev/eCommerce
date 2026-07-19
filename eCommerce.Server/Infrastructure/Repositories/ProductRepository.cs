using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Domain.Repositories;
using eCommerce.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Server.Infrastructure.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetActivePagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default)
    {
        var filteredQuery = ApplyQueryFilters(db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .AsQueryable(), query);

        return await filteredQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(ProductQueryParams query, CancellationToken cancellationToken = default)
    {
        var filteredQuery = ApplyQueryFilters(db.Products
            .Where(p => p.IsActive)
            .AsQueryable(), query);

        return filteredQuery.CountAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        db.Products
          .Include(p => p.Images)
          .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public Task<Product?> GetActiveByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        db.Products
          .Include(p => p.Category)
          .Include(p => p.Images)
          .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);

    public Task<Product?> GetActiveBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Products
          .Include(p => p.Category)
          .Include(p => p.Images)
          .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, cancellationToken);

    public Task<Product?> GetByIdWithImagesAsync(int productId, CancellationToken cancellationToken = default) =>
        db.Products
          .Include(p => p.Images)
          .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public Task<ProductImage?> GetImageAsync(int productId, int imageId, CancellationToken cancellationToken = default) =>
        db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        db.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Slug == slug && p.Id != excludeId, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        db.Products.AddAsync(product, cancellationToken).AsTask();

    public void Remove(Product product) => db.Products.Remove(product);

    public void RemoveImage(ProductImage image) => db.ProductImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    private static IQueryable<Product> ApplyQueryFilters(IQueryable<Product> queryable, ProductQueryParams query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
            queryable = queryable.Where(p => p.Name.Contains(query.Search) || (p.Brand != null && p.Brand.Contains(query.Search)));

        if (query.CategoryId.HasValue)
            queryable = queryable.Where(p => p.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Brand))
            queryable = queryable.Where(p => p.Brand == query.Brand);

        if (query.MinPrice.HasValue)
            queryable = queryable.Where(p => p.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            queryable = queryable.Where(p => p.Price <= query.MaxPrice.Value);

        if (query.InStock == true)
            queryable = queryable.Where(p => p.StockQuantity > 0);

        if (query.Featured == true)
            queryable = queryable.Where(p => p.IsFeatured);

        queryable = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
        {
            ("price", "asc") => queryable.OrderBy(p => p.Price),
            ("price", _) => queryable.OrderByDescending(p => p.Price),
            ("name", "asc") => queryable.OrderBy(p => p.Name),
            ("name", _) => queryable.OrderByDescending(p => p.Name),
            _ => query.SortDir.ToLower() == "asc"
                ? queryable.OrderBy(p => p.CreatedAt)
                : queryable.OrderByDescending(p => p.CreatedAt)
        };

        return queryable;
    }
}
