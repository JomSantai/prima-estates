using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace PrimaEstates.Services;

public interface IImageStorage
{
    Task<string?> SaveAsync(IFormFile file);
}

/// <summary>
/// Uploads to Cloudinary when credentials are configured (production),
/// otherwise falls back to saving under wwwroot/uploads (local dev).
/// </summary>
public class ImageStorage : IImageStorage
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileBytes = 5 * 1024 * 1024;

    private readonly Cloudinary? _cloudinary;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageStorage> _logger;

    public ImageStorage(IConfiguration config, IWebHostEnvironment env, ILogger<ImageStorage> logger)
    {
        _env = env;
        _logger = logger;

        var raw = config["CLOUDINARY_URL"]
                  ?? config["Cloudinary:Url"]
                  ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL");

        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogWarning("IMAGE STORAGE: CLOUDINARY_URL is not set. "
                + "Uploads will be saved to local disk and WILL BE LOST on redeploy.");
            return;
        }

        // Strip stray whitespace, wrapping quotes, and an accidental "NAME=" prefix -
        // all of these are invisible in dashboard UIs but break the scheme check.
        var url = raw.Trim().Trim('"', '\'');
        if (url.StartsWith("CLOUDINARY_URL=", StringComparison.OrdinalIgnoreCase))
            url = url["CLOUDINARY_URL=".Length..].Trim().Trim('"', '\'');

        if (!url.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
        {
            // Show only the leading, non-sensitive portion so the cause is visible.
            var head = url.Length > 14 ? url[..14] : url;
            _logger.LogError("IMAGE STORAGE: CLOUDINARY_URL does not start with 'cloudinary://'. "
                + "Raw length {RawLen}, cleaned length {Len}, starts with: '{Head}'. "
                + "Falling back to local disk.", raw.Length, url.Length, head);
            return;
        }

        try
        {
            _cloudinary = new Cloudinary(url) { Api = { Secure = true } };
            _logger.LogInformation("IMAGE STORAGE: Cloudinary configured for cloud '{Cloud}'.",
                _cloudinary.Api?.Account?.Cloud ?? "unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IMAGE STORAGE: could not initialise Cloudinary; "
                + "falling back to local disk.");
        }
    }

    public async Task<string?> SaveAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxFileBytes) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) return null;

        if (_cloudinary != null)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "prima-estates",
                UniqueFilename = true,
                Overwrite = false
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return result.SecureUrl?.ToString();

            _logger.LogError("Cloudinary upload failed: {Error}", result.Error?.Message);
            return null;
        }

        // Local fallback
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await using var fileStream = File.Create(fullPath);
        await file.CopyToAsync(fileStream);
        return $"/uploads/{fileName}";
    }
}
