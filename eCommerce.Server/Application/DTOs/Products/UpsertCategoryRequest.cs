using System.ComponentModel.DataAnnotations;

namespace eCommerce.Server.Application.DTOs.Products;

public class UpsertCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
