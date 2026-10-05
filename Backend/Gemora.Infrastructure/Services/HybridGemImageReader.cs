
using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Services;

/// <summary>
/// Supports both legacy local gemstone images
/// and new Supabase Storage gemstone images.
/// </summary>
public sealed class HybridGemImageReader : IGemImageReader
{
    private const string LocalGemImagePrefix =
        "/uploads/gem-images/";

    private readonly SupabaseStorageClient _supabaseStorage;

    private readonly LocalGemImageReader _localImageReader;

    public HybridGemImageReader(
        SupabaseStorageClient supabaseStorage,
        LocalGemImageReader localImageReader)
    {
        _supabaseStorage = supabaseStorage;

        _localImageReader = localImageReader;
    }

    // ============================================================
    // OPEN GEMSTONE IMAGE
    // ============================================================

    public async Task<GemImageReadResult?> OpenImageAsync(
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        // ========================================================
        // OPTION 1 — LEGACY LOCAL IMAGE
        //
        // Example:
        // /uploads/gem-images/image.jpg
        // ========================================================

        if (imageUrl.StartsWith(
                LocalGemImagePrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return await _localImageReader.OpenImageAsync(
                imageUrl,
                cancellationToken
            );
        }

        // ========================================================
        // OPTION 2 — SUPABASE IMAGE
        //
        // The Supabase client validates that this reference
        // belongs to this project's gem-images bucket.
        //
        // Arbitrary external URLs are rejected.
        // ========================================================

        if (!Uri.TryCreate(
                imageUrl,
                UriKind.Absolute,
                out var imageUri))
        {
            return null;
        }

        var imageBytes =
            await _supabaseStorage.DownloadGemImageAsync(
                imageUrl,
                cancellationToken
            );

        if (imageBytes == null || imageBytes.Length == 0)
        {
            return null;
        }

        // ========================================================
        // DETERMINE THE IMAGE CONTENT TYPE
        // ========================================================

        var extension =
            Path.GetExtension(
                imageUri.AbsolutePath
            ).ToLowerInvariant();

        var contentType = extension switch
        {
            ".jpg" => "image/jpeg",

            ".jpeg" => "image/jpeg",

            ".png" => "image/png",

            ".webp" => "image/webp",

            _ => null
        };

        if (contentType == null)
        {
            return null;
        }

        // ========================================================
        // RETURN AN IN-MEMORY STREAM TO THE AI AGENT
        //
        // The caller owns and disposes GemImageReadResult.
        // ========================================================

        var imageStream = new MemoryStream(
            imageBytes,
            writable: false
        );

        return new GemImageReadResult(
            imageStream,
            contentType
        );
    }
}
