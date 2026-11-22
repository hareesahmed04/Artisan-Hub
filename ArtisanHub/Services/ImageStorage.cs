namespace ArtisanHub.Services;

/// <summary>Saves product photos uploaded by artists into wwwroot/uploads/products.</summary>
public class ImageStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IWebHostEnvironment _environment;

    public ImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public static string? Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return $"\"{file.FileName}\" is not a supported image. Use JPG, PNG, WEBP or GIF.";
        }

        if (file.Length > MaxBytes)
        {
            return $"\"{file.FileName}\" is larger than 5 MB.";
        }

        return null;
    }

    public async Task<string> SaveProductImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/products/{fileName}";
    }

    public void DeleteIfUploaded(string imageUrl)
    {
        if (!imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;

        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, relative);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
