using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Application.DTOs.Products;
using eCommerce.Server.Application.Helpers;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Infrastructure.Data;
using eCommerce.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db, ImageUploadService imageService) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams query)
    {
        var q = db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.Contains(query.Search) || p.Brand!.Contains(query.Search));

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Brand))
            q = q.Where(p => p.Brand == query.Brand);

        if (query.MinPrice.HasValue)
            q = q.Where(p => p.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.Price <= query.MaxPrice.Value);

        if (query.InStock == true)
            q = q.Where(p => p.StockQuantity > 0);

        if (query.Featured == true)
            q = q.Where(p => p.IsFeatured);

        q = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
        {
            ("price", "asc")  => q.OrderBy(p => p.Price),
            ("price", _)      => q.OrderByDescending(p => p.Price),
            ("name", "asc")   => q.OrderBy(p => p.Name),
            ("name", _)       => q.OrderByDescending(p => p.Name),
            _                 => query.SortDir.ToLower() == "asc"
                                    ? q.OrderBy(p => p.CreatedAt)
                                    : q.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    // GET api/products/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        return product is null ? NotFound() : Ok(ToDto(product));
    }

    // GET api/products/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        return product is null ? NotFound() : Ok(ToDto(product));
    }

    // POST api/products  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive))
            return BadRequest("Category not found.");

        var slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name));

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            StockQuantity = request.StockQuantity,
            Brand = request.Brand,
            Sku = request.Sku,
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured,
            CategoryId = request.CategoryId
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new { product.Id, product.Slug });
    }

    // PUT api/products/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive))
            return BadRequest("Category not found.");

        product.Name = request.Name;
        product.Slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name), id);
        product.Description = request.Description;
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.StockQuantity = request.StockQuantity;
        product.Brand = request.Brand;
        product.Sku = request.Sku;
        product.IsActive = request.IsActive;
        product.IsFeatured = request.IsFeatured;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/products/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        foreach (var img in product.Images)
            imageService.DeleteImage(img.Url);

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/products/5/images  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var product = await db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        string url;
        try { url = await imageService.SaveImageAsync(file); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }

        var isPrimary = !product.Images.Any();
        var image = new ProductImage
        {
            ProductId = id,
            Url = url,
            AltText = product.Name,
            IsPrimary = isPrimary,
            SortOrder = product.Images.Count
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync();

        return Ok(new ProductImageDto
        {
            Id = image.Id, Url = image.Url,
            AltText = image.AltText, IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        });
    }

    // DELETE api/products/5/images/3  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int productId, int imageId)
    {
        var image = await db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

        if (image is null) return NotFound();

        imageService.DeleteImage(image.Url);
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        Price = p.Price,
        CompareAtPrice = p.CompareAtPrice,
        StockQuantity = p.StockQuantity,
        Brand = p.Brand,
        Sku = p.Sku,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto
        {
            Id = i.Id, Url = i.Url,
            AltText = i.AltText, IsPrimary = i.IsPrimary,
            SortOrder = i.SortOrder
        })
    };

    private async Task<string> UniqueSlugAsync(string baseSlug, int? excludeId = null)
    {
        var slug = baseSlug;
        var counter = 1;
        while (await db.Products.AnyAsync(p => p.Slug == slug && p.Id != excludeId))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }
}
