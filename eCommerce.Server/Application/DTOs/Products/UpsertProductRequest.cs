using System.ComponentModel.DataAnnotations;

namespace eCommerce.Server.Application.DTOs.Products;

public class UpsertProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? CompareAtPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Sku { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
