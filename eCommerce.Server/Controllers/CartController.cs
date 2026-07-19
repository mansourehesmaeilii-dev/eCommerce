using System.Security.Claims;
using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Cart;
using eCommerce.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController(ICartService cartService) : ControllerBase
{
    // ── GET api/cart ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    // ── POST api/cart/items ──────────────────────────────────────────────────
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await cartService.AddItemAsync(GetUserId(), request);
        return ToActionResult(result);
    }

    // ── PUT api/cart/items/{itemId} ──────────────────────────────────────────
    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await cartService.UpdateItemAsync(GetUserId(), itemId, request);
        return ToActionResult(result);
    }

    // ── DELETE api/cart/items/{itemId} ───────────────────────────────────────
    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var result = await cartService.RemoveItemAsync(GetUserId(), itemId);
        return ToActionResult(result);
    }

    // ── DELETE api/cart ──────────────────────────────────────────────────────
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await cartService.ClearCartAsync(GetUserId());
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private IActionResult ToActionResult(Result<CartDto> result) =>
        result.Success
            ? Ok(result.Data)
            : result.StatusCode switch
            {
                StatusCodes.Status404NotFound => NotFound(result.Error),
                StatusCodes.Status400BadRequest => BadRequest(result.Error),
                _ => StatusCode(result.StatusCode, result.Error)
            };
}
