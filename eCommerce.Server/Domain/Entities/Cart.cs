using eCommerce.Server.Domain.Common;

namespace eCommerce.Server.Domain.Entities;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = [];

    public DomainResult AddItem(Product product, int quantity)
    {
        if (!product.IsActive)
            return DomainResult.NotFound("Product not found.");

        if (quantity < 1)
            return DomainResult.Validation("Quantity must be at least 1.");

        if (product.StockQuantity < quantity)
            return DomainResult.Validation($"Only {product.StockQuantity} unit(s) available.");

        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (newQuantity > product.StockQuantity)
                return DomainResult.Validation($"Only {product.StockQuantity} unit(s) available.");

            existingItem.Quantity = newQuantity;
        }
        else
        {
            Items.Add(new CartItem
            {
                CartId = Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price,
                Product = product
            });
        }

        Touch();
        return DomainResult.Success();
    }

    public DomainResult UpdateItemQuantity(int itemId, int quantity, int stockQuantity)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return DomainResult.NotFound("Item not found in cart.");

        if (quantity < 1)
            return DomainResult.Validation("Quantity must be at least 1.");

        if (quantity > stockQuantity)
            return DomainResult.Validation($"Only {stockQuantity} unit(s) available.");

        item.Quantity = quantity;
        Touch();
        return DomainResult.Success();
    }

    public DomainResult RemoveItem(int itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return DomainResult.NotFound("Item not found in cart.");

        Items.Remove(item);
        Touch();
        return DomainResult.Success();
    }

    public void ClearItems()
    {
        if (!Items.Any()) return;
        Items.Clear();
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
