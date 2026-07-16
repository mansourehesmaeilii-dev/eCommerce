namespace eCommerce.Server.Application.DTOs.Cart;

public class CartDto
{
    public int Id { get; set; }
    public IEnumerable<CartItemDto> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int TotalItems => Items.Sum(i => i.Quantity);
}
