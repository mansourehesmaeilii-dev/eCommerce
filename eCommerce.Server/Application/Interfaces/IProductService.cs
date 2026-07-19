using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Application.DTOs.Products;
using Microsoft.AspNetCore.Http;

namespace eCommerce.Server.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQueryParams query);
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<Result<ProductDto>> GetBySlugAsync(string slug);
    Task<Result<ProductCreateResultDto>> CreateAsync(UpsertProductRequest request);
    Task<Result> UpdateAsync(int id, UpsertProductRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result<ProductImageDto>> UploadImageAsync(int productId, IFormFile file);
    Task<Result> DeleteImageAsync(int productId, int imageId);
}
