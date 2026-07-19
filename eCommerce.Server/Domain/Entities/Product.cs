using eCommerce.Server.Domain.Common;

namespace eCommerce.Server.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }   // original price for "sale" display
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? Sku { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<ProductImage> Images { get; set; } = [];

    public void UpdateDetails(
        string name,
        string slug,
        string description,
        decimal price,
        decimal? compareAtPrice,
        int stockQuantity,
        string? brand,
        string? sku,
        bool isActive,
        bool isFeatured,
        int categoryId)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Price = price;
        CompareAtPrice = compareAtPrice;
        StockQuantity = stockQuantity;
        Brand = brand;
        Sku = sku;
        IsActive = isActive;
        IsFeatured = isFeatured;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public ProductImage AddImage(string url)
    {
        var image = new ProductImage
        {
            ProductId = Id,
            Url = url,
            AltText = Name,
            IsPrimary = !Images.Any(),
            SortOrder = Images.Count
        };

        Images.Add(image);
        UpdatedAt = DateTime.UtcNow;
        return image;
    }

    public DomainResult RemoveImage(int imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return DomainResult.NotFound("Image not found.");

        Images.Remove(image);
        UpdatedAt = DateTime.UtcNow;
        return DomainResult.Success();
    }
}
