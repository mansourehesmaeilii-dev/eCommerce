namespace eCommerce.Server.Infrastructure.Services;

public class ImageUploadService(IWebHostEnvironment env, ILogger<ImageUploadService> logger)
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<string> SaveImageAsync(IFormFile file, string subfolder = "products")
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 5 MB size limit.");

        var uploadsRoot = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", subfolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        logger.LogInformation("Saved image {FileName}", fileName);
        return $"/uploads/{subfolder}/{fileName}";
    }

    public void DeleteImage(string relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl)) return;
        var fullPath = Path.Combine(env.WebRootPath ?? "wwwroot",
            relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
