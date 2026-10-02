
using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Services;

/// <summary>
/// Stores new gemstone images in Supabase Storage.
///
/// During migration:
/// - New gem images use Supabase.
/// - Certificates continue using existing local storage.
/// - Existing local files can still be deleted.
/// </summary>
public sealed class HybridFileStorageService : IFileStorageService
{
    private const long MaxGemImageSize =
        5L * 1024 * 1024;

    private readonly SupabaseStorageClient _supabaseStorage;

    private readonly LocalFileStorageService _localStorage;

    public HybridFileStorageService(
        SupabaseStorageClient supabaseStorage,
        LocalFileStorageService localStorage)
    {
        _supabaseStorage = supabaseStorage;

        _localStorage = localStorage;
    }

    // ============================================================
    // SAVE GEMSTONE IMAGE TO SUPABASE
    // ============================================================

    public async Task<string> SaveGemImageAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        if (fileLength <= 0)
        {
            throw new InvalidOperationException(
                "Please select a gemstone image to upload."
            );
        }

        if (fileLength > MaxGemImageSize)
        {
            throw new InvalidOperationException(
                "The gemstone image must be 5 MB or smaller."
            );
        }

        if (fileStream == null || !fileStream.CanRead)
        {
            throw new InvalidOperationException(
                "The selected gemstone image cannot be read."
            );
        }

        // --------------------------------------------------------
        // Validate extension and MIME type together.
        // --------------------------------------------------------

        var extension =
            Path.GetExtension(fileName).ToLowerInvariant();

        var expectedContentType = extension switch
        {
            ".jpg" => "image/jpeg",

            ".jpeg" => "image/jpeg",

            ".png" => "image/png",

            ".webp" => "image/webp",

            _ => null
        };

        if (expectedContentType == null)
        {
            throw new InvalidOperationException(
                "Please upload a JPG, PNG or WebP gemstone image."
            );
        }

        if (!string.Equals(
                contentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The image format does not match its file extension."
            );
        }

        // --------------------------------------------------------
        // Never use the original filename as the storage key.
        // --------------------------------------------------------

        var objectName =
            $"{Guid.NewGuid():N}{extension}";

        // --------------------------------------------------------
        // Upload to the PUBLIC gem-images Supabase bucket.
        //
        // The client independently enforces its size limit.
        // --------------------------------------------------------

        var imageUrl =
            await _supabaseStorage.UploadAsync(
                bucket: "gem-images",
                objectName: objectName,
                source: fileStream,
                contentType: expectedContentType,
                maximumBytes: MaxGemImageSize
            );

        return imageUrl;
    }

    // ============================================================
    // CERTIFICATE UPLOAD
    //
    // Temporary migration behavior:
    //
    // Keep the existing implementation until the private
    // certificate access endpoint is implemented.
    // ============================================================

    public Task<string> SaveCertificateAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        return _localStorage.SaveCertificateAsync(
            fileStream,
            fileName,
            contentType,
            fileLength
        );
    }

    // ============================================================
    // DELETE FILE
    //
    // Supports both:
    //
    // 1. Existing /uploads/... references.
    // 2. New Supabase object URLs.
    //
    // SupabaseStorageClient validates Supabase references
    // before allowing deletion.
    // ============================================================

    public async Task DeleteFileAsync(
        string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        // Existing local file.

        if (fileUrl.StartsWith(
                "/uploads/",
                StringComparison.OrdinalIgnoreCase))
        {
            await _localStorage.DeleteFileAsync(
                fileUrl
            );

            return;
        }

        // New Supabase file.

        await _supabaseStorage.DeleteAsync(
            fileUrl
        );
    }
}
