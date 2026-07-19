using eCommerce.Server.Application.DTOs.Cart;
using eCommerce.Server.Application.Common.Results;

namespace eCommerce.Server.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<Result<CartDto>> AddItemAsync(string userId, AddToCartRequest request);
    Task<Result<CartDto>> UpdateItemAsync(string userId, int itemId, UpdateCartItemRequest request);
    Task<Result<CartDto>> RemoveItemAsync(string userId, int itemId);
    Task ClearCartAsync(string userId);
}
