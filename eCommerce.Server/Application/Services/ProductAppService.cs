using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Application.DTOs.Products;
using eCommerce.Server.Application.Helpers;
using eCommerce.Server.Application.Interfaces;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Domain.Repositories;
using Microsoft.AspNetCore.Http;

namespace eCommerce.Server.Application.Services;

public class ProductAppService(
    IProductRepository productRepository,
    IImageUploadService imageUploadService) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQueryParams query)
    {
        var totalCount = await productRepository.CountActiveAsync(query);
        var products = await productRepository.GetActivePagedAsync(query);

        return new PagedResult<ProductDto>
        {
            Items = products.Select(ToDto),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id)
    {
        var product = await productRepository.GetActiveByIdAsync(id);
        return product is null
            ? Result<ProductDto>.Fail(StatusCodes.Status404NotFound, "Product not found.")
            : Result<ProductDto>.Ok(ToDto(product));
    }

    public async Task<Result<ProductDto>> GetBySlugAsync(string slug)
    {
        var product = await productRepository.GetActiveBySlugAsync(slug);
        return product is null
            ? Result<ProductDto>.Fail(StatusCodes.Status404NotFound, "Product not found.")
            : Result<ProductDto>.Ok(ToDto(product));
    }

    public async Task<Result<ProductCreateResultDto>> CreateAsync(UpsertProductRequest request)
    {
        if (!await productRepository.CategoryExistsAsync(request.CategoryId))
            return Result<ProductCreateResultDto>.Fail(StatusCodes.Status400BadRequest, "Category not found.");

        var slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name));

        var product = new Product();
        product.UpdateDetails(
            request.Name,
            slug,
            request.Description,
            request.Price,
            request.CompareAtPrice,
            request.StockQuantity,
            request.Brand,
            request.Sku,
            request.IsActive,
            request.IsFeatured,
            request.CategoryId);

        await productRepository.AddAsync(product);
        await productRepository.SaveChangesAsync();

        return Result<ProductCreateResultDto>.Ok(new ProductCreateResultDto
        {
            Id = product.Id,
            Slug = product.Slug
        }, StatusCodes.Status201Created);
    }

    public async Task<Result> UpdateAsync(int id, UpsertProductRequest request)
    {
        var product = await productRepository.GetByIdAsync(id);
        if (product is null)
            return Result.Fail(StatusCodes.Status404NotFound, "Product not found.");

        if (!await productRepository.CategoryExistsAsync(request.CategoryId))
            return Result.Fail(StatusCodes.Status400BadRequest, "Category not found.");

        var slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name), id);

        product.UpdateDetails(
            request.Name,
            slug,
            request.Description,
            request.Price,
            request.CompareAtPrice,
            request.StockQuantity,
            request.Brand,
            request.Sku,
            request.IsActive,
            request.IsFeatured,
            request.CategoryId);

        await productRepository.SaveChangesAsync();
        return Result.Ok(StatusCodes.Status204NoContent);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var product = await productRepository.GetByIdWithImagesAsync(id);
        if (product is null)
            return Result.Fail(StatusCodes.Status404NotFound, "Product not found.");

        foreach (var image in product.Images)
            imageUploadService.DeleteImage(image.Url);

        productRepository.Remove(product);
        await productRepository.SaveChangesAsync();
        return Result.Ok(StatusCodes.Status204NoContent);
    }

    public async Task<Result<ProductImageDto>> UploadImageAsync(int productId, IFormFile file)
    {
        var product = await productRepository.GetByIdWithImagesAsync(productId);
        if (product is null)
            return Result<ProductImageDto>.Fail(StatusCodes.Status404NotFound, "Product not found.");

        string url;
        try
        {
            url = await imageUploadService.SaveImageAsync(file);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProductImageDto>.Fail(StatusCodes.Status400BadRequest, ex.Message);
        }

        var image = product.AddImage(url);
        await productRepository.SaveChangesAsync();

        return Result<ProductImageDto>.Ok(ToImageDto(image));
    }

    public async Task<Result> DeleteImageAsync(int productId, int imageId)
    {
        var product = await productRepository.GetByIdWithImagesAsync(productId);
        if (product is null)
            return Result.Fail(StatusCodes.Status404NotFound, "Product not found.");

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return Result.Fail(StatusCodes.Status404NotFound, "Image not found.");

        imageUploadService.DeleteImage(image.Url);
        var removeImageResult = product.RemoveImage(imageId);
        if (!removeImageResult.IsSuccess)
            return Result.Fail(StatusCodes.Status404NotFound, removeImageResult.Error ?? "Image not found.");

        productRepository.RemoveImage(image);
        await productRepository.SaveChangesAsync();
        return Result.Ok(StatusCodes.Status204NoContent);
    }

    private async Task<string> UniqueSlugAsync(string baseSlug, int? excludeId = null)
    {
        var slug = baseSlug;
        var counter = 1;
        while (await productRepository.SlugExistsAsync(slug, excludeId))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }

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
        Images = p.Images.OrderBy(i => i.SortOrder).Select(ToImageDto)
    };

    private static ProductImageDto ToImageDto(ProductImage i) => new()
    {
        Id = i.Id,
        Url = i.Url,
        AltText = i.AltText,
        IsPrimary = i.IsPrimary,
        SortOrder = i.SortOrder
    };
}
