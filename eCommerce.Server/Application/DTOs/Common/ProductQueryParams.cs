namespace eCommerce.Server.Application.DTOs.Common;

public class ProductQueryParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 12;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? Featured { get; set; }

    /// <summary>name | price | created</summary>
    public string SortBy { get; set; } = "created";
    /// <summary>asc | desc</summary>
    public string SortDir { get; set; } = "desc";
}
