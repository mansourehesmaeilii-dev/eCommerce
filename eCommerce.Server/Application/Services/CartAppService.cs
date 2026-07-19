using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Cart;
using eCommerce.Server.Application.Interfaces;
using eCommerce.Server.Domain.Common;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Domain.Repositories;
using Microsoft.AspNetCore.Http;

namespace eCommerce.Server.Application.Services;

public class CartAppService(
    ICartRepository cartRepository,
    IProductRepository productRepository) : ICartService
{
    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return ToDto(cart);
    }

    public async Task<Result<CartDto>> AddItemAsync(string userId, AddToCartRequest request)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId);
        if (product is null || !product.IsActive)
            return Result<CartDto>.Fail(StatusCodes.Status404NotFound, "Product not found.");

        var cart = await GetOrCreateCartAsync(userId);

        var domainResult = cart.AddItem(product, request.Quantity);
        if (!domainResult.IsSuccess)
            return ToFailureResult(domainResult);

        await cartRepository.SaveChangesAsync();

        var refreshed = await cartRepository.GetByUserIdWithItemsAsync(userId) ?? cart;
        return Result<CartDto>.Ok(ToDto(refreshed));
    }

    public async Task<Result<CartDto>> UpdateItemAsync(string userId, int itemId, UpdateCartItemRequest request)
    {
        var cart = await cartRepository.GetByUserIdWithItemsAsync(userId);
        if (cart is null)
            return Result<CartDto>.Fail(StatusCodes.Status404NotFound, "Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result<CartDto>.Fail(StatusCodes.Status404NotFound, "Item not found in cart.");

        var product = await productRepository.GetByIdAsync(item.ProductId);
        if (product is null)
            return Result<CartDto>.Fail(StatusCodes.Status404NotFound, "Product no longer exists.");

        var domainResult = cart.UpdateItemQuantity(itemId, request.Quantity, product.StockQuantity);
        if (!domainResult.IsSuccess)
            return ToFailureResult(domainResult);

        await cartRepository.SaveChangesAsync();
        return Result<CartDto>.Ok(ToDto(cart));
    }

    public async Task<Result<CartDto>> RemoveItemAsync(string userId, int itemId)
    {
        var cart = await cartRepository.GetByUserIdWithItemsAsync(userId);
        if (cart is null)
            return Result<CartDto>.Fail(StatusCodes.Status404NotFound, "Cart not found.");

        var domainResult = cart.RemoveItem(itemId);
        if (!domainResult.IsSuccess)
            return ToFailureResult(domainResult);

        await cartRepository.SaveChangesAsync();

        var refreshed = await cartRepository.GetByUserIdWithItemsAsync(userId) ?? cart;
        return Result<CartDto>.Ok(ToDto(refreshed));
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await cartRepository.GetByUserIdWithItemsAsync(userId);
        if (cart is null) return;

        cart.ClearItems();
        await cartRepository.SaveChangesAsync();
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await cartRepository.GetByUserIdWithItemsAsync(userId);
        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        await cartRepository.AddAsync(cart);
        await cartRepository.SaveChangesAsync();

        return await cartRepository.GetByUserIdWithItemsAsync(userId) ?? cart;
    }

    private static Result<CartDto> ToFailureResult(DomainResult domainResult)
    {
        var statusCode = domainResult.ErrorCode switch
        {
            DomainErrorCode.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Result<CartDto>.Fail(statusCode, domainResult.Error ?? "Operation failed.");
    }

    private static CartDto ToDto(Cart cart) => new()
    {
        Id = cart.Id,
        Items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            ProductSlug = i.Product.Slug,
            ProductImageUrl = i.Product.Images
                               .OrderBy(img => img.SortOrder)
                               .FirstOrDefault(img => img.IsPrimary)?.Url
                           ?? i.Product.Images
                               .OrderBy(img => img.SortOrder)
                               .FirstOrDefault()?.Url,
            Brand = i.Product.Brand,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            StockQuantity = i.Product.StockQuantity
        })
    };
}
