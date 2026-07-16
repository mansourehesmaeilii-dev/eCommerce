namespace eCommerce.Server.Application.DTOs.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? Sku { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public IEnumerable<ProductImageDto> Images { get; set; } = [];
    public string? PrimaryImageUrl => Images.FirstOrDefault(i => i.IsPrimary)?.Url
                                   ?? Images.FirstOrDefault()?.Url;
}
