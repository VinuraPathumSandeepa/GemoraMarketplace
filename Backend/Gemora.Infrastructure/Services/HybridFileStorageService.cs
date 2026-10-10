using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Services;

/// <summary>
/// Hybrid storage implementation used during the Gemora
/// migration to persistent Supabase Storage.
///
/// New files:
///
/// - Gemstone images
///     -> Public Supabase "gem-images" bucket.
///
/// - Certificates
///     -> Private Supabase "gem-certificates" bucket.
///
/// Existing legacy files:
///
/// - /uploads/... references
///     -> Continue using LocalFileStorageService for cleanup.
///
/// This allows existing database records to continue working
/// while all new evidence is stored persistently in Supabase.
/// </summary>
public sealed class HybridFileStorageService : IFileStorageService
{
    // ============================================================
    // SIZE LIMITS
    // ============================================================

    private const long MaxGemImageSize =
        5L * 1024 * 1024;

    private const long MaxCertificateSize =
        10L * 1024 * 1024;

    // ============================================================
    // SUPABASE BUCKETS
    // ============================================================

    private const string GemImagesBucket =
        "gem-images";

    private const string CertificatesBucket =
        "gem-certificates";

    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private readonly SupabaseStorageClient _supabaseStorage;

    private readonly LocalFileStorageService _localStorage;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public HybridFileStorageService(
        SupabaseStorageClient supabaseStorage,
        LocalFileStorageService localStorage)
    {
        _supabaseStorage =
            supabaseStorage
            ?? throw new ArgumentNullException(
                nameof(supabaseStorage)
            );

        _localStorage =
            localStorage
            ?? throw new ArgumentNullException(
                nameof(localStorage)
            );
    }

    // ============================================================
    // SAVE GEMSTONE IMAGE TO SUPABASE
    //
    // Bucket:
    // gem-images
    //
    // Visibility:
    // Public
    //
    // PostgreSQL stores:
    //
    // https://PROJECT.supabase.co/storage/v1/object/public/
    // gem-images/filename.jpg
    // ============================================================

    public async Task<string> SaveGemImageAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        // --------------------------------------------------------
        // 1. VALIDATE SIZE
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // 2. VALIDATE STREAM
        // --------------------------------------------------------

        if (
            fileStream == null ||
            !fileStream.CanRead
        )
        {
            throw new InvalidOperationException(
                "The selected gemstone image cannot be read."
            );
        }

        // --------------------------------------------------------
        // 3. VALIDATE FILENAME
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException(
                "The gemstone image filename is invalid."
            );
        }

        // --------------------------------------------------------
        // 4. VALIDATE EXTENSION AND CONTENT TYPE
        // --------------------------------------------------------

        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        var expectedContentType =
            extension switch
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
        // 5. GENERATE SAFE STORAGE OBJECT NAME
        //
        // Never use the Seller's original filename.
        // --------------------------------------------------------

        var objectName =
            $"{Guid.NewGuid():N}{extension}";

        // --------------------------------------------------------
        // 6. UPLOAD TO PUBLIC SUPABASE GEM IMAGE BUCKET
        // --------------------------------------------------------

        var imageUrl =
            await _supabaseStorage.UploadAsync(
                bucket: GemImagesBucket,
                objectName: objectName,
                source: fileStream,
                contentType: expectedContentType,
                maximumBytes: MaxGemImageSize
            );

        // --------------------------------------------------------
        // 7. RETURN PUBLIC SUPABASE URL
        // --------------------------------------------------------

        return imageUrl;
    }

    // ============================================================
    // SAVE CERTIFICATE TO PRIVATE SUPABASE STORAGE
    //
    // Bucket:
    // gem-certificates
    //
    // Visibility:
    // PRIVATE
    //
    // PostgreSQL stores:
    //
    // supabase-private://gem-certificates/filename.pdf
    //
    // The permanent Storage URL is never exposed publicly.
    //
    // GemCertificatesController checks authorization and creates
    // a temporary signed URL when the Seller or Gemologist needs
    // to view the certificate.
    // ============================================================

    public async Task<string> SaveCertificateAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        // --------------------------------------------------------
        // 1. VALIDATE SIZE
        // --------------------------------------------------------

        if (fileLength <= 0)
        {
            throw new InvalidOperationException(
                "Please select a certificate file to upload."
            );
        }

        if (fileLength > MaxCertificateSize)
        {
            throw new InvalidOperationException(
                "The certificate file must be 10 MB or smaller."
            );
        }

        // --------------------------------------------------------
        // 2. VALIDATE STREAM
        // --------------------------------------------------------

        if (
            fileStream == null ||
            !fileStream.CanRead
        )
        {
            throw new InvalidOperationException(
                "The selected certificate file cannot be read."
            );
        }

        // --------------------------------------------------------
        // 3. VALIDATE FILENAME
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException(
                "The certificate filename is invalid."
            );
        }

        // --------------------------------------------------------
        // 4. VALIDATE EXTENSION AND MIME TYPE
        //
        // Supported:
        //
        // PDF
        // JPG / JPEG
        // PNG
        // --------------------------------------------------------

        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        var expectedContentType =
            extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".jpg" =>
                    "image/jpeg",

                ".jpeg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                _ => null
            };

        if (expectedContentType == null)
        {
            throw new InvalidOperationException(
                "Please upload a PDF, JPG or PNG certificate file."
            );
        }

        if (!string.Equals(
                contentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The certificate format does not match its file extension."
            );
        }

        // --------------------------------------------------------
        // 5. GENERATE RANDOM STORAGE OBJECT NAME
        //
        // Example:
        //
        // 58d72ef284414f4c9019617c5a95210f.pdf
        //
        // Do not expose the Seller's original filename.
        // --------------------------------------------------------

        var objectName =
            $"{Guid.NewGuid():N}{extension}";

        // --------------------------------------------------------
        // 6. UPLOAD TO PRIVATE SUPABASE BUCKET
        //
        // SupabaseStorageClient:
        //
        // - enforces the certificate bucket
        // - checks extension/MIME agreement
        // - enforces the 10 MB certificate limit
        // - returns a private reference instead of a public URL
        // --------------------------------------------------------

        var certificateReference =
            await _supabaseStorage.UploadAsync(
                bucket: CertificatesBucket,
                objectName: objectName,
                source: fileStream,
                contentType: expectedContentType,
                maximumBytes: MaxCertificateSize
            );

        // --------------------------------------------------------
        // 7. RETURN PRIVATE REFERENCE
        //
        // Example:
        //
        // supabase-private://gem-certificates/...
        //
        // This value is safe to store in PostgreSQL.
        // --------------------------------------------------------

        return certificateReference;
    }

    // ============================================================
    // DELETE FILE
    //
    // Supports:
    //
    // 1. Legacy local references
    //
    //    /uploads/gem-images/...
    //    /uploads/certificates/...
    //
    // 2. Public Supabase image URLs
    //
    // 3. Private certificate references
    //
    //    supabase-private://gem-certificates/...
    // ============================================================

    public async Task DeleteFileAsync(
        string? fileUrl)
    {
        // --------------------------------------------------------
        // 1. NOTHING TO DELETE
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        // --------------------------------------------------------
        // 2. LEGACY LOCAL FILE
        // --------------------------------------------------------

        if (fileUrl.StartsWith(
                "/uploads/",
                StringComparison.OrdinalIgnoreCase))
        {
            await _localStorage.DeleteFileAsync(
                fileUrl
            );

            return;
        }

        // --------------------------------------------------------
        // 3. SUPABASE FILE
        //
        // SupabaseStorageClient independently validates that the
        // supplied reference belongs to an allowed Gemora bucket.
        //
        // It supports:
        //
        // - public gem image URLs
        // - public profile image URLs
        // - private certificate references
        // --------------------------------------------------------

        await _supabaseStorage.DeleteAsync(
            fileUrl
        );
    }
}