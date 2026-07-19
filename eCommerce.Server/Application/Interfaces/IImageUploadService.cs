using Microsoft.AspNetCore.Http;

namespace eCommerce.Server.Application.Interfaces;

public interface IImageUploadService
{
    Task<string> SaveImageAsync(IFormFile file, string subfolder = "products");
    void DeleteImage(string relativeUrl);
}
