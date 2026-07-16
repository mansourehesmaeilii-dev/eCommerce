using System.ComponentModel.DataAnnotations;

namespace eCommerce.Server.Application.DTOs.Cart;

public class UpdateCartItemRequest
{
    [Required, Range(1, 100)]
    public int Quantity { get; set; }
}
