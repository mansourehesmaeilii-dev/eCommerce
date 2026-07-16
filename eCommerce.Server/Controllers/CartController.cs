using System.Security.Claims;
using eCommerce.Server.Application.DTOs.Cart;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController(AppDbContext db) : ControllerBase
{
    // ── GET api/cart ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);
        return Ok(ToDto(cart));
    }

    // ── POST api/cart/items ──────────────────────────────────────────────────
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();

        var product = await db.Products.FindAsync(request.ProductId);
        if (product is null || !product.IsActive)
            return NotFound("Product not found.");

        if (product.StockQuantity < request.Quantity)
            return BadRequest($"Only {product.StockQuantity} unit(s) available.");

        var cart = await GetOrCreateCartAsync(userId);

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            var newQty = existingItem.Quantity + request.Quantity;
            if (newQty > product.StockQuantity)
                return BadRequest($"Only {product.StockQuantity} unit(s) available.");

            existingItem.Quantity = newQty;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Reload with navigation properties for response
        await db.Entry(cart).Collection(c => c.Items).LoadAsync();
        return Ok(ToDto(cart));
    }

    // ── PUT api/cart/items/{itemId} ──────────────────────────────────────────
    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var cart = await GetCartWithItemsAsync(userId);
        if (cart is null) return NotFound("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return NotFound("Item not found in cart.");

        var product = await db.Products.FindAsync(item.ProductId);
        if (product is null) return NotFound("Product no longer exists.");

        if (request.Quantity > product.StockQuantity)
            return BadRequest($"Only {product.StockQuantity} unit(s) available.");

        item.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(cart));
    }

    // ── DELETE api/cart/items/{itemId} ───────────────────────────────────────
    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var userId = GetUserId();
        var cart = await GetCartWithItemsAsync(userId);
        if (cart is null) return NotFound("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return NotFound("Item not found in cart.");

        db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Return updated cart
        var refreshed = await GetCartWithItemsAsync(userId);
        return Ok(ToDto(refreshed!));
    }

    // ── DELETE api/cart ──────────────────────────────────────────────────────
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var cart = await GetCartWithItemsAsync(userId);
        if (cart is null) return NoContent();

        db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await GetCartWithItemsAsync(userId);
        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return cart;
    }

    private Task<Cart?> GetCartWithItemsAsync(string userId) =>
        db.Carts
          .Include(c => c.Items)
              .ThenInclude(i => i.Product)
                  .ThenInclude(p => p.Images)
          .FirstOrDefaultAsync(c => c.UserId == userId);

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
